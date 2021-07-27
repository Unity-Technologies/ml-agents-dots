import os
import time
import uuid
from typing import Tuple, Dict

from mlagents_dots_envs.shared_memory.shared_memory_header import SharedMemoryHeader
from mlagents_dots_envs.shared_memory.shared_memory_body import SharedMemoryBody

from mlagents_envs.exception import UnityCommunicationException
from mlagents_envs.base_env import (
    DecisionSteps,
    TerminalSteps,
    BehaviorSpec,
    ActionTuple,
)


class SharedMemoryCommunicator:
    FILE_DEFAULT = "default"

    def __init__(self, use_default: bool = False, timeout_wait: int = 60):
        if use_default:
            file_name = self.FILE_DEFAULT
        else:
            file_name = str(uuid.uuid1())
            while os.path.exists(file_name):
                file_name = str(uuid.uuid1())
        self._base_file_name = file_name
        self._master_mem = SharedMemoryHeader(file_name=file_name)
        self._current_file_number = self._master_mem.file_number
        self._data_mem = SharedMemoryBody(
            file_name + "_" * self._current_file_number,
            create_file=True,
            copy_from=None,
            side_channel_buffer_size=4,
            rl_data_buffer_size=0,
        )
        self._timeout_wait = timeout_wait

    @property
    def communicator_id(self):
        return self._base_file_name

    def close(self):
        self._master_mem.close()
        self._data_mem.delete()

    @property
    def active(self) -> bool:
        return self._master_mem.active

    def write_side_channel_data(self, data: bytearray) -> None:
        capacity = self._master_mem.side_channel_size
        if len(data) >= capacity - 4:  # need 4 bytes for an integer size
            new_capacity = 2 * len(data) + 20
            self._current_file_number += 1
            self._master_mem.file_number = self._current_file_number
            tmp = self._data_mem
            self._data_mem = SharedMemoryBody(
                self._base_file_name + "_" * self._current_file_number,
                create_file=True,
                copy_from=tmp,
                side_channel_buffer_size=new_capacity,
                rl_data_buffer_size=self._master_mem.rl_data_size,
            )
            tmp.close()
            # Unity is responsible for destroying the old file
            self._master_mem.side_channel_size = new_capacity
        self._data_mem.side_channel_data = data

    def read_and_clear_side_channel_data(self) -> bytearray:
        result = self._data_mem.side_channel_data
        self._data_mem.side_channel_data = bytearray()
        return result

    def give_unity_control(self, reset: bool = False, query: bool = False) -> None:
        self._master_mem.mark_python_blocked()
        if query:
            self._master_mem.mark_query()
        if reset:
            self._master_mem.mark_reset()
        self._master_mem.unblock_unity()

    def wait_for_unity(self):
        iteration = 0
        check_timeout_iteration = 1000
        t0 = time.time()
        while self._master_mem.blocked and self._master_mem.active:
            if iteration % check_timeout_iteration == 0:
                if time.time() - t0 > self._timeout_wait:
                    self.close()
                    self._master_mem.delete()
                    raise TimeoutError("The Unity Environment took too long to respond")
            iteration += 1
        if not self._master_mem.active:
            try:
                self._master_mem.check_version()
            finally:
                self._master_mem.delete()
                self._data_mem.delete()
                raise UnityCommunicationException("Communicator has stopped.")
        while self._current_file_number < self._master_mem.file_number:
            # the file is out of date
            self._data_mem.delete()
            self._current_file_number += 1
            self._data_mem = SharedMemoryBody(
                self._base_file_name + "_" * self._current_file_number,
                side_channel_buffer_size=self._master_mem.side_channel_size,
                rl_data_buffer_size=self._master_mem.rl_data_size,
            )

    def get_steps(self, key: str) -> Tuple[DecisionSteps, TerminalSteps]:
        return (
            self._data_mem.get_decision_steps(key),
            self._data_mem.get_terminal_steps(key),
        )

    def get_n_decisions_requested(self, key: str) -> int:
        return self._data_mem.get_n_decisions_requested(key)

    def set_actions(self, key: str, data: ActionTuple) -> None:
        self._data_mem.set_actions(key, data)

    @property
    def num_behaviors(self) -> int:
        return self._data_mem.num_behaviors

    def generate_specs(self) -> Dict[str, BehaviorSpec]:
        return self._data_mem.generate_specs()
