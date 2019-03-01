import mmap
import struct
import numpy as np
import time

class UnityCommunication:
    FILE_CAPACITY = 200000
    NUMBER_AGENTS_POSITION = 0
    SENSOR_SIZE_POSITION = 4
    ACTUATOR_SIZE_POSITION = 8
    UNITY_READY_POSITION = 12
    SENSOR_DATA_POSITION = 13

    PYTHON_READY_POSITION = 100000
    ACTUATOR_DATA_POSITION = 100001

    FILE_NAME = "shared_communication_file.txt"

    def __init__(self):
        with open(self.FILE_NAME, "r+b") as f:
            # memory-map the file, size 0 means whole file
            self.accessor = mmap.mmap(f.fileno(), 0)

    def get_int(self, position : int) -> int:
        return struct.unpack("i", self.accessor[position:position + 4])[0]

    def read_sensor(self) -> np.ndarray:
        sensor_size = self.get_int(self.SENSOR_SIZE_POSITION)
        number_agents = self.get_int(self.NUMBER_AGENTS_POSITION)

        sensor = np.frombuffer(
            buffer=self.accessor[self.SENSOR_DATA_POSITION: self.SENSOR_DATA_POSITION + 4*sensor_size*number_agents],
            dtype=np.float32,
            count=sensor_size * number_agents,
            offset=0
        )
        return np.reshape(sensor, (number_agents, sensor_size))

    def get_parameters(self) -> (int, int, int):
        return self.get_int(self.NUMBER_AGENTS_POSITION), \
               self.get_int(self.SENSOR_SIZE_POSITION), \
               self.get_int(self.ACTUATOR_SIZE_POSITION)

    def write_actuator(self, actuator: np.ndarray):
        actuator_size = self.get_int(self.ACTUATOR_SIZE_POSITION)
        number_agents = self.get_int(self.NUMBER_AGENTS_POSITION)

        # TODO : Support more types ?
        if actuator.dtype != np.float32:
            actuator = actuator.astype(np.float32)

        assert(actuator.shape == (number_agents, actuator_size))

        self.accessor[self.ACTUATOR_DATA_POSITION: self.ACTUATOR_DATA_POSITION + 4*actuator_size*number_agents] = \
            actuator.tobytes()

    def set_ready(self, flag : bool):
        self.accessor[self.PYTHON_READY_POSITION: self.PYTHON_READY_POSITION+1] = bytearray(struct.pack("b", flag))

    def unity_ready(self) -> bool:
        return self.accessor[self.UNITY_READY_POSITION]

    def close(self):
        self.accessor.close()


if __name__ == "__main__":
    comm = UnityCommunication()

    steps = 0
    while True:

        u_ready = False
        while not u_ready:
            u_ready = comm.unity_ready()
        steps += 1
        s = comm.read_sensor()
        nag, nse, nac = comm.get_parameters()
        time.sleep(0.1)
        comm.write_actuator(
            np.random.normal(size=(nag, nac))
        )
        comm.set_ready(True)









