from mlagents_dots_envs.base_shared_mem import BasedSharedMem

class MasterSharedMem(BasedSharedMem):
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
    SIZE = 28
    VERSION = (0,3,0)
    def __init__(self, file_name:str, side_channel_size = 0, rl_data_size=0):
        super(MasterSharedMem, self).__init__(file_name, self.SIZE, True)
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

    @property
    def active(self) -> bool:
        offset = 15
        result, _ = self.get_bool(offset)
        return result

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
        offset = 15
        self.set_bool(offset, True)
        super(MasterSharedMem, self).close()

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

    @property
    def side_channel_size(self):
        offset = 20
        return self.get_int(offset)

    @side_channel_size.setter
    def side_channel_size(self, value:int):
        offset = 20
        self.set_int(offset, value)

    @property
    def rl_data_size(self):
        offset = 24
        return self.get_int(offset)

    @rl_data_size.setter
    def rl_data_size(self, value:int):
        offset = 24
        self.set_int(offset, value)

