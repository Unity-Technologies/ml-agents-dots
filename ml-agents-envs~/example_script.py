from unity_environment import UnityEnvironment

from mlagents_envs.side_channel.engine_configuration_channel import (
    EngineConfigurationChannel,
)

sc = EngineConfigurationChannel()
env = UnityEnvironment(side_channels=[sc])

env.reset()


for i in range(10):
	s = ""
	for name in env.get_agent_groups():
		s += name +" : " + str(env.get_step_result(name).n_agents()) +" | "
	print(s)
	env.step()
print("RESET")
env.reset()

for i in range(10):
        s = ""
        for name in env.get_agent_groups():
                s += name +" : " + str(env.get_step_result(name).n_agents()) +" | "
        print(s)
        env.step()
env.close()

