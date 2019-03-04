import mmap
import struct
import numpy as np

from .brain import BrainInfo, BrainParameters


class ECSEnvironment(object):
    VEC_SIZE = 8
    ACT_SIZE = 3

    def __init__(self):
        self.comm = UnityCommunication()
        self.step_count = 0
        print(" READY TO COMMUNICATE")

    def reset(self, train_mode=True, config=None, lesson=None):
        u_ready = False
        while not u_ready:
            u_ready = self.comm.unity_ready()
        s = self.comm.read_sensor()
        return self.make_brain_info(s)

    def step(self, vector_action=None, memory=None, text_action=None, value=None):
        # print("step")
        self.comm.write_actuator(vector_action["ECSBrain"])
        self.comm.set_ready()

        u_ready = False
        while not u_ready:
            u_ready = self.comm.unity_ready()
        s = self.comm.read_sensor()
        return self.make_brain_info(s)

    def close(self):
        self.comm.close()

    def make_brain_info(self, sensor):
        # This assumes the order is consistent
        self.step_count+=1
        done = False
        if self.step_count % 50 == 0:
            done = True
        return {"ECSBrain" : BrainInfo([], sensor, [" "] * sensor.shape[0],
                         reward=sensor[:,0],
                         agents=list(range(sensor.shape[0])),
                         local_done=[done] * sensor.shape[0],
                         max_reached=[done] * sensor.shape[0],
                         vector_action=sensor,
                         text_action = [" "] * sensor.shape[0])}


    @property
    def curriculum(self):
        return None

    @property
    def logfile_path(self):
        return None

    @property
    def brains(self):
        return {"ECSBrain": BrainParameters("ECSBrain", self.VEC_SIZE, 1,
                 [], [self.ACT_SIZE],
                 [" "]*self.ACT_SIZE, 1)} # 1 for continuopus

    @property
    def global_done(self):
        return False

    @property
    def academy_name(self):
        return "ECSAcademy"

    @property
    def number_brains(self):
        return 1

    @property
    def number_external_brains(self):
        return 1

    @property
    def brain_names(self):
        return ["ECSBrain"]

    @property
    def external_brain_names(self):
        return ["ECSBrain"]





class UnityCommunication:
    FILE_CAPACITY = 200000
    NUMBER_AGENTS_POSITION = 0
    SENSOR_SIZE_POSITION = 4
    ACTUATOR_SIZE_POSITION = 8
    UNITY_READY_POSITION = 12
    SENSOR_DATA_POSITION = 13

    PYTHON_READY_POSITION = 100000
    ACTUATOR_DATA_POSITION = 100001

    # FILE_NAME = "../../../ml-agents-ecs/Assets/shared_communication_file.txt"
    FILE_NAME = "shared_communication_file.txt" # This is relative to where the script is called

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

        try:
            assert(actuator.shape == (number_agents, actuator_size))
        except:
            print("_________")
            print(actuator.shape)
            print((number_agents, actuator_size))

        self.accessor[self.ACTUATOR_DATA_POSITION: self.ACTUATOR_DATA_POSITION + 4*actuator_size*number_agents] = \
            actuator.tobytes()

    def set_ready(self):
        self.accessor[self.UNITY_READY_POSITION: self.UNITY_READY_POSITION+1] = bytearray(struct.pack("b", False))
        self.accessor[self.PYTHON_READY_POSITION: self.PYTHON_READY_POSITION+1] = bytearray(struct.pack("b", True))

    def unity_ready(self) -> bool:
        return self.accessor[self.UNITY_READY_POSITION]

    def close(self):
        self.accessor.close()


# if __name__ == "__main__":
#     comm = UnityCommunication()
#
#     steps = 0
#     while True:
#
#         u_ready = False
#         while not u_ready:
#             u_ready = comm.unity_ready()
#         steps += 1
#         s = comm.read_sensor()
#         nag, nse, nac = comm.get_parameters()
#         # print(s.shape)
#         # time.sleep(0.1)
#         comm.write_actuator(
#             np.random.normal(size=(nag, nac))
#         )
#         comm.set_ready()
#


