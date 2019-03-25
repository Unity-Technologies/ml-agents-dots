import mmap
import struct
import numpy as np

from python_communication import UnityCommunication
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


