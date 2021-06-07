import numpy as np
import argparse
from mlagents_dots_envs.unity_environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import (
    EngineConfigurationChannel,
)
from mlagents_envs.side_channel.environment_parameters_channel import (
    EnvironmentParametersChannel,
)
from mlagents_envs.logging_util import set_log_level, DEBUG


set_log_level(DEBUG)


def perform_steps(n):
    for _ in range(n):
        s = ""
        for name, specs in env.behavior_specs.items():
            s += (
                name
                + " : "
                + str(len(env.get_steps(name)[0]))
                + " : "
                + str(len(env.get_steps(name)[1]))
                + "|"
            )
            env.set_actions(
                name, np.ones((len(env.get_steps(name)[0]), specs.action_size))
            )
        print(s)
        env.step()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--env", default=None, type=str, help="The width of the image to display."
    )
    args = parser.parse_args()

    sc = EngineConfigurationChannel()
    sc2 = EnvironmentParametersChannel()
    env = UnityEnvironment(file_name=args.env, side_channels=[sc, sc2])
    sc.set_configuration_parameters()

    print("RESET")
    env.reset()

    perform_steps(100)

    print("RESET")
    env.reset()

    sc2.set_float_parameter("test", 2)
    sc2.set_float_parameter("test2", 2)
    sc2.set_float_parameter("test3", 2)

    perform_steps(100)

    env.close()
