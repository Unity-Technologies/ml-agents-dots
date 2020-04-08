import mmap
import struct
import numpy as np
import tempfile
import os
import time
import uuid
from datetime import datetime
import string
from enum import IntEnum
import random
from typing import Tuple, Optional, NamedTuple, List, Dict

from mlagents_dots_envs.master_shared_mem import MasterSharedMem
from mlagents_dots_envs.data_shared_mem import DataSharedMem

from mlagents_envs.exception import UnityCommunicationException
from mlagents_envs.base_env import DecisionSteps, TerminalSteps, BehaviorSpec

class SharedMemCom:
    FILE_DEFAULT = "default"
    MAX_TIMEOUT_IN_SECONDS = 5

    def __init__(self, use_default: bool = False):
        if use_default:
            file_name = self.FILE_DEFAULT
        else:
            file_name = str(uuid.uuid4())
            while os.path.exists(file_name):
                file_name = str(uuid.uuid4())
        self._base_file_name = file_name
        self._master_mem = MasterSharedMem(file_name=file_name)
        self._current_file_number = self._master_mem.file_number
        self._data_mem = DataSharedMem(file_name + "_" * self._current_file_number, create_file=True)

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
            self._current_file_number += 1
            self._master_mem.file_number = self._current_file_number
            self._data_mem = DataSharedMem(self._base_file_name + "_" * self._current_file_number,
                create_file=True,
                copy_from=self._data_mem,
                side_channel_buffer_size= 2*len(data)+20
            )
            # Unity is responsible for destroying the old file
        self._data_mem.side_channel_data = data

    def read_side_channel_data_and_clear(self) -> bytearray:
        result = self._data_mem.side_channel_data
        self._data_mem.side_channel_data = bytearray()
        return result

    def give_unity_control(self, reset:bool = False):
        self._master_mem.mark_python_blocked()
        if reset:
            self._master_mem.mark_reset()
        self._master_mem.unblock_unity()

    def wait_for_unity(self):
        iteration = 0
        check_timeout_iteration = 1000
        t0 = time.time()
        while self._master_mem.blocked and self._master_mem.active:
            if iteration % check_timeout_iteration == 0:
                if time.time() - t0 > self.MAX_TIMEOUT_IN_SECONDS:
                    self.close()
                    raise TimeoutError("The Unity Environment took too long to respond")
            iteration += 1
        if not self._master_mem.active:
            self._data_mem.delete()
            self._data_mem.delete()
            raise UnityCommunicationException("Communicator has stopped.")
        if self._current_file_number != self._master_mem.file_number:
            # the file is out of date
            self._data_mem.delete()
            self._current_file_number = self._master_mem.file_number
            self._data_mem = DataSharedMem(self._base_file_name + "_" * self._current_file_number)

    def get_steps(self, key: str) -> Tuple[DecisionSteps, TerminalSteps]:
        return self._data_mem.get_decision_steps(key), self._data_mem.get_terminal_steps(key)

    def get_n_decisions_requested(self, key: str) -> int:
        return self._data_mem.get_n_decisions_requested(key)

    def set_actions(self, key: str, data:np.ndarray) -> None:
        self._data_mem.set_actions(key, data)

    @property
    def num_behaviors(self) -> int:
        return self._data_mem.num_behaviors

    def generate_specs(self) -> Dict[str, BehaviorSpec]:
        return self._data_mem.generate_specs()
