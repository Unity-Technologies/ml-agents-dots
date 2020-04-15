import numpy as np

from mlagents_dots_envs.unity_environment import UnityEnvironment

from mlagents_envs.side_channel.engine_configuration_channel import (
    EngineConfigurationChannel,
)
from mlagents_envs.side_channel.float_properties_channel import FloatPropertiesChannel

sc = EngineConfigurationChannel()
sc2 = FloatPropertiesChannel()
env = UnityEnvironment(side_channels=[sc, sc2])
sc.set_configuration_parameters()


for _ in range(10):
    s = ""
    for name in env.get_behavior_names():
        s += (
            name
            + " : "
            + str(len(env.get_steps(name)[0]))
            + " : "
            + str(len(env.get_steps(name)[1]))
            + "|"
        )
        env.set_actions(name, np.ones((len(env.get_steps(name)[0]), 2)))
    print(s)
    env.step()

print(env.get_steps("Ball_DOTS")[0].obs[0])

print("RESET")
env.reset()


sc2.set_property("test", 2)
sc2.set_property("test2", 2)
sc2.set_property("test3", 2)

for _ in range(100):
    s = ""
    for name in env.get_behavior_names():
        s += (
            name
            + " : "
            + str(len(env.get_steps(name)[0]))
            + " : "
            + str(len(env.get_steps(name)[1]))
            + "|"
        )
    print(s)
    env.step()
env.close()
