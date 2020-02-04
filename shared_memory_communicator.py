import mmap
import struct
import numpy as np
import tempfile
import os
import time
from datetime import datetime
import string
from enum import IntEnum
import random
from typing import Tuple, Optional, NamedTuple, List, Dict


class AgentGroupFileOffsets(NamedTuple):
    """
    Contains the offsets to the data for a specific AgentGroup
    """

    is_continuous: bool
    obs_shapes: List[Tuple[int, ...]]
    a_size: int
    discrete_branches: Optional[Tuple[int, ...]]
    max_n_agents: int
    n_agents_offset: int
    obs_offset: Tuple[int, ...]
    rewards_offset: int
    done_offset: int
    max_step_offset: int
    agent_id_offset: int
    masks_offset: Optional[int]
    action_offset: int


class PythonCommand(IntEnum):
    DEFAULT = 0
    RESET = 1
    CHANGE_FILE = 2
    CLOSE = 3


class SharedMemoryCom:
    API_VERSION = 0
    DIRECTORY = "ml-agents"
    FILE_DEFAULT = "default"
    STRING_LEN = 64
    FILE_LENGTH_OFFSET = 0
    VERSION_OFFSET = 4
    MUTEXT_OFFSET = 8  # Unity blocked = True, Python Blocked = False
    COMMAND_OFFSET = 9
    SIDE_CHANNEL_CAPACITY_OFFSET = 10
    MAX_TIMEOUT_IN_SECONDS = 30

    def __init__(self, use_default: bool = False):
        directory = os.path.join(tempfile.gettempdir(), self.DIRECTORY)
        if not os.path.exists(directory):
            os.makedirs(directory)
        if use_default:
            file_name = os.path.join(directory, self.FILE_DEFAULT)
        else:
            letters = string.ascii_letters
            file_id = "".join(random.choice(letters) for i in range(20))
            file_name = file_name = os.path.join(directory, file_id)
            while os.path.exists(file_name):
                file_id = "".join(random.choice(letters) for i in range(20))
                file_name = file_name = os.path.join(directory, file_id)

        self.file_name = file_name
        data = bytearray(
            struct.pack(
                "<ii?Biii", 22, self.API_VERSION, False, PythonCommand.DEFAULT, 4, 0, 0
            )
        )
        self._create_file(self.file_name, data)

        with open(self.file_name, "r+b") as f:
            # memory-map the file, size 0 means whole file
            self.accessor = mmap.mmap(f.fileno(), 0)

        self.group_offsets: Dict[str, AgentGroupFileOffsets] = {}
        self._group_offsets_dirty = False

    def close(self):
        if self.accessor is not None:
            self.wait_for_unity()
            self.give_unity_control(PythonCommand.CLOSE)
            self.accessor.close()
            self.accessor = None

    def get_file_name(self) -> None:
        return self.file_name

    @property
    def active(self) -> bool:
        return self.accessor is not None

    def write_side_channel_data(self, data: bytearray) -> None:
        capacity = self._get_side_channel_capacity(self.accessor)
        if len(data) >= capacity - 4:
            # 4 if for the int giving the actual size of payload
            self._create_next_file(2 * len(data) + 20)
            self._refresh_agent_group_offsets()
        struct.pack_into(
            "<i", self.accessor, self.SIDE_CHANNEL_CAPACITY_OFFSET + 4, len(data)
        )
        start = self.SIDE_CHANNEL_CAPACITY_OFFSET + 8
        self.accessor[start : start + len(data)] = data

    def read_side_channel_data_and_clear(self) -> bytearray:
        message_len = struct.unpack_from(
            "<i", self.accessor, self.SIDE_CHANNEL_CAPACITY_OFFSET + 4
        )[0]
        if message_len == 0:
            return bytearray()
        else:
            start = self.SIDE_CHANNEL_CAPACITY_OFFSET + 8
            result = bytearray(self.accessor[start : start + message_len])
            struct.pack_into(
                "<i", self.accessor, self.SIDE_CHANNEL_CAPACITY_OFFSET + 4, 0
            )
            self.accessor[start : start + message_len] = bytearray(message_len)
            return result

    def give_unity_control(self, command: PythonCommand):
        struct.pack_into("<B", self.accessor, self.COMMAND_OFFSET, command)
        struct.pack_into("<?", self.accessor, self.MUTEXT_OFFSET, True)

    def wait_for_unity(self):
        self._wait_for_unity_helper()
        if self._group_offsets_dirty:
            self._refresh_agent_group_offsets()
            self._group_offsets_dirty = False

    def set_ndarray(self, offset: int, data: np.ndarray) -> None:
        bytes_data = data.tobytes()
        self.accessor[offset : offset + len(bytes_data)] = bytes_data

    def get_ndarray(
        self, offset: int, shape: Tuple[int, ...], t: np.dtype
    ) -> np.ndarray:
        return np.frombuffer(
            buffer=self.accessor, dtype=t, count=np.prod(shape), offset=offset
        ).reshape(shape)

    def _wait_for_unity_helper(self):
        iteration = 0
        max_loop = 1000000
        t0 = time.time()
        while struct.unpack_from("<?", self.accessor, self.MUTEXT_OFFSET)[0]:
            if iteration % max_loop == 0:
                if time.time() - t0 > self.MAX_TIMEOUT_IN_SECONDS:
                    self.close()
                    raise TimeoutError("The Unity Environment took too long to respond")
            iteration += 1
        self._apply_unity_command()

    def _create_file(self, file_name: str, data: bytearray) -> None:
        with open(file_name, "w+b") as f:
            f.write(data)

    def _apply_unity_command(self):
        command = struct.unpack_from("<B", self.accessor, self.COMMAND_OFFSET)[0]
        if command == PythonCommand.CLOSE:
            # CLOSE COMMAND
            self.accessor.close()
            self.accessor = None
            try:
                os.remove(self.file_name)
            except:
                pass
            self.file_name = None
        elif command == PythonCommand.CHANGE_FILE:
            self._delete_file_and_move_to_new_file()
            self._group_offsets_dirty = True
            self._wait_for_unity_helper()

    def _clear_side_channel(self):
        capacity = self._get_side_channel_capacity(self.accessor)
        start = self.SIDE_CHANNEL_CAPACITY_OFFSET + 4
        self.accessor[start : start + capacity] = bytearray(capacity)

    def _create_next_file(self, side_channel_new_capacity: int) -> None:
        # Will never need to increase the RL data capacity
        # Is only triggered by a too large side channel data
        new_file_name = self.file_name + "_"
        side_channel_old_capacity = self._get_side_channel_capacity(self.accessor)
        self._clear_side_channel()
        data = bytearray(self.accessor)
        start = self.SIDE_CHANNEL_CAPACITY_OFFSET + 4
        data[start:start] = bytearray(
            side_channel_new_capacity - side_channel_old_capacity
        )
        struct.pack_into(
            "<i", data, self.SIDE_CHANNEL_CAPACITY_OFFSET, side_channel_new_capacity
        )
        struct.pack_into("<i", data, 0, len(data))
        self._create_file(new_file_name, data)

        self.file_name = new_file_name
        # we give Unity control over the old file but not the new one
        self.give_unity_control(PythonCommand.CHANGE_FILE)

        self.accessor.close()
        with open(new_file_name, "r+b") as f:
            # memory-map the file, size 0 means whole file
            self.accessor = mmap.mmap(f.fileno(), 0)

    def _delete_file_and_move_to_new_file(self) -> None:
        # Only when Unity wants to change the file
        self.accessor.close()
        new_file_name = self.file_name + "_"
        with open(new_file_name, "r+b") as f:
            self.accessor = mmap.mmap(f.fileno(), 0)
        os.remove(self.file_name)
        self.file_name = new_file_name

    @staticmethod
    def _get_side_channel_capacity(acc) -> int:
        return struct.unpack_from(
            "<i", acc, SharedMemoryCom.SIDE_CHANNEL_CAPACITY_OFFSET
        )[0]

    @staticmethod
    def _get_agent_group_section_offset(accessor) -> int:
        sc_capacity = SharedMemoryCom._get_side_channel_capacity(accessor)
        return SharedMemoryCom.SIDE_CHANNEL_CAPACITY_OFFSET + 4 + sc_capacity

    @staticmethod
    def _get_int(accessor, offset) -> Tuple[int, int]:
        return struct.unpack_from("<i", accessor, offset)[0], offset + 4

    @staticmethod
    def _get_float(accessor, offset) -> Tuple[float, int]:
        return struct.unpack_from("<f", accessor, offset)[0], offset + 4

    @staticmethod
    def _get_bool(accessor, offset) -> Tuple[bool, int]:
        return struct.unpack_from("<?", accessor, offset)[0], offset + 1

    @staticmethod
    def _get_string(accessor, offset) -> Tuple[str, int]:
        string_len = struct.unpack_from("<B", accessor, offset)[0]
        byte_array = bytes(accessor[offset + 1 : offset + string_len + 1])
        result = byte_array.decode("ascii")
        return result, offset + SharedMemoryCom.STRING_LEN

    def get_n_agent_groups(self):
        offset = self._get_agent_group_section_offset(self.accessor)
        n_groups, _ = self._get_int(self.accessor, offset)
        return n_groups

    def get_n_agents(self, group_name: str) -> int:
        result, _ = self._get_int(
            self.accessor, self.group_offsets[group_name].n_agents_offset
        )
        return result

    def _refresh_agent_group_offsets(self):
        # Generates the offsets and returns the GroupSpecs
        # AgentGroupSpec is represented as :
        # string : fixed size : policy name
        # int : 4 bytes : maximum number of Agents
        # bool : 1 byte : is action discrete (False) or continuous (True)
        # int : 4 bytes : action space size (continuous) / number of branches (discrete)
        # -- If discrete only : array of action sizes for each branch
        # int : 4 bytes : number of observations
        # For each observation :
        # 3 int : shape (the shape of the tensor observation for one agent
        # start of the section that will change every step
        # 4 bytes : n_agents at current step
        # ? Bytes : the data : obs,reward,done,max_step,agent_id,masks,action
        offset = self._get_agent_group_section_offset(self.accessor)
        n_groups, offset = self._get_int(self.accessor, offset)

        self.group_offsets.clear()

        for _ in range(n_groups):
            # Get the specs of the group
            group_name, offset = self._get_string(self.accessor, offset)

            max_n_agents, offset = self._get_int(self.accessor, offset)
            is_continuous, offset = self._get_bool(self.accessor, offset)
            a_size, offset = self._get_int(self.accessor, offset)
            discrete_branches = None
            if not is_continuous:
                discrete_branches = ()
                for _ in range(a_size):
                    branch_size, offset = self._get_int(self.accessor, offset)
                    discrete_branches += (branch_size,)
            n_obs, offset = self._get_int(self.accessor, offset)
            obs_shapes: List[Tuple[int, ...]] = []
            for _ in range(n_obs):
                shape = ()
                for _ in range(3):
                    s, offset = self._get_int(self.accessor, offset)
                    if s != 0:
                        shape += (s,)
                obs_shapes += [shape]
            #  Compute the offsets :
            # n_agents
            n_agents_offset = offset
            offset += 4
            # observations
            obs_offset: Tuple[int, ...] = ()
            for s in obs_shapes:
                obs_offset += (offset,)
                offset += 4 * max_n_agents * np.prod(s)
            # rewards
            rew_offset = offset
            offset += 4 * max_n_agents
            # done
            don_offset = offset
            offset += max_n_agents
            # max step reached
            max_step_offset = offset
            offset += max_n_agents
            # agent id
            id_offset = offset
            offset += 4 * max_n_agents
            # mask
            if is_continuous:
                mask_offset = None
            else:
                mask_offset = offset
                offset += max_n_agents * np.sum(discrete_branches)
            # action
            act_offset = offset
            offset += 4 * max_n_agents * a_size
            self.group_offsets[group_name] = AgentGroupFileOffsets(
                is_continuous=is_continuous,
                obs_shapes=obs_shapes,
                a_size=a_size,
                discrete_branches=discrete_branches,
                max_n_agents=max_n_agents,
                n_agents_offset=n_agents_offset,
                obs_offset=obs_offset,
                rewards_offset=rew_offset,
                done_offset=don_offset,
                max_step_offset=max_step_offset,
                agent_id_offset=id_offset,
                masks_offset=mask_offset,
                action_offset=act_offset,
            )

