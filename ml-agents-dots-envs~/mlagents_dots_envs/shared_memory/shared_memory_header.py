import os
import glob
from mlagents_dots_envs.shared_memory.base_shared_memory import BaseSharedMemory


class SharedMemoryHeader(BaseSharedMemory):
    """
    Always created by Python
    File organization:
     - 3 ints : major, minor, bug version
     - bool : True if Simulation is blocked
     - bool : True if Python is blocked
     - bool : True if Python commanded a reset
     - bool : True if Simulation or Python ordered closing the communication
     - int  : The number of times the communication file changed
     - int  : Communication file "side channel" size in bytes
     - int  : Communication file "RL section" size in bytes
    """

    SIZE = 29
    VERSION = (0, 3, 2)

    def __init__(
        self, file_name: str, side_channel_size: int = 0, rl_data_size: int = 0
    ):

        super(SharedMemoryHeader, self).__init__(
            file_name, create_file=True, size=self.SIZE
        )
        for f in glob.glob(self._file_path + "_*"):
            # Removing all the future files in case they were not correctly created
            os.remove(f)
        offset = 0
        offset = self.set_int(offset, self.VERSION[0])
        offset = self.set_int(offset, self.VERSION[1])
        offset = self.set_int(offset, self.VERSION[2])
        offset = self.set_bool(offset, False)
        offset = self.set_bool(offset, False)
        offset = self.set_bool(offset, False)
        offset = self.set_bool(offset, False)
        offset = self.set_int(offset, 1)
        offset = self.set_int(offset, side_channel_size)
        offset = self.set_int(offset, rl_data_size)
        offset = self.set_bool(offset, False)

    @property
    def active(self) -> bool:
        offset = 15
        closed, _ = self.get_bool(offset)
        return not closed

    @property
    def file_number(self) -> int:
        offset = 16
        result, _ = self.get_int(offset)
        return result

    @file_number.setter
    def file_number(self, value: int) -> None:
        offset = 16
        self.set_int(offset, value)

    def close(self):
        if self.accessor is not None:
            offset = 15
            self.set_bool(offset, True)
        super(SharedMemoryHeader, self).close()

    def mark_python_blocked(self):
        offset = 13
        self.set_bool(offset, True)

    @property
    def blocked(self) -> bool:
        offset = 13
        result, _ = self.get_bool(offset)
        return result

    def unblock_unity(self):
        offset = 12
        self.set_bool(offset, False)

    def mark_reset(self):
        offset = 14
        self.set_bool(offset, True)

    def mark_query(self):
        offset = 28
        self.set_bool(offset, True)

    @property
    def side_channel_size(self) -> int:
        offset = 20
        result, _ = self.get_int(offset)
        return result

    @side_channel_size.setter
    def side_channel_size(self, value: int) -> None:
        offset = 20
        self.set_int(offset, value)

    @property
    def rl_data_size(self) -> int:
        offset = 24
        result, _ = self.get_int(offset)
        return result

    @rl_data_size.setter
    def rl_data_size(self, value: int) -> None:
        offset = 24
        self.set_int(offset, value)

    def check_version(self):
        offset = 0
        major, offset = self.get_int(offset)
        minor, offset = self.get_int(offset)
        bug, offset = self.get_int(offset)
        if (major, minor, bug) != self.VERSION:
            raise Exception(
                "Incompatible versions of communicator between "
                + f"Unity {major}.{minor}.{bug} and Python "
                f"{self.VERSION[0]}.{self.VERSION[1]}.{self.VERSION[2]}"
            )
