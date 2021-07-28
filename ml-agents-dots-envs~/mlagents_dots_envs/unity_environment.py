import atexit
import numpy as np
import subprocess
from typing import List, Optional, Tuple

from mlagents_envs.side_channel.side_channel import SideChannel

from mlagents_envs.base_env import (
    BaseEnv,
    DecisionSteps,
    TerminalSteps,
    BehaviorName,
    BehaviorMapping,
    ActionTuple,
)
from mlagents_envs.timers import timed
from mlagents_envs.exception import UnityCommunicationException, UnityActionException

from mlagents_dots_envs.shared_memory.shared_memory_communicator import (
    SharedMemoryCommunicator,
)

from mlagents_envs.side_channel.side_channel_manager import SideChannelManager
from mlagents_envs.env_utils import launch_executable

from mlagents_envs.logging_util import get_logger
from mlagents_envs.communicator_objects.capabilities_pb2 import UnityRLCapabilitiesProto


logger = get_logger(__name__)


class UnityEnvironment(BaseEnv):
    API_VERSION = "API-14"  # TODO : REMOVE
    DEFAULT_EDITOR_PORT = 5004  # TODO : REMOVE
    BASE_ENVIRONMENT_PORT = 5005  # TODO : REMOVE

    def __init__(
        self,
        file_name: Optional[str] = None,
        side_channels: Optional[List[SideChannel]] = None,
        additional_args: Optional[List[str]] = None,
        timeout_wait: int = 60,
        worker_id: Optional[int] = None,  # TODO : REMOVE
        seed: Optional[int] = None,  # TODO : REMOVE
        no_graphics: Optional[bool] = None,  # TODO : REMOVE
        base_port: Optional[int] = None,  # TODO : REMOVE
        log_folder: Optional[str] = None,  # TODO : REMOVE
    ):
        """
        Starts a new unity environment and establishes a connection with it.

        :string file_name: Name of Unity environment binary. If None, will try to
        connect to the Editor.
        :list args: Addition Unity command line arguments
        :list side_channels: Additional side channel for not-rl communication with Unity
        """
        self.academy_capabilities = UnityRLCapabilitiesProto()  # TODO : REMOVE
        self.academy_capabilities.baseRLCapabilities = True
        self.academy_capabilities.concatenatedPngObservations = True
        self.academy_capabilities.compressedChannelMapping = True
        self.academy_capabilities.hybridActions = True
        self.academy_capabilities.trainingAnalytics = True
        self.academy_capabilities.variableLengthObservation = True
        self.academy_capabilities.multiAgentGroups = True

        args = additional_args or []
        atexit.register(self.close)
        executable_name = file_name

        editor_connect = executable_name is None
        if no_graphics is not None and no_graphics and not editor_connect:
            args += ["-nographics", "-batchmode"]

        if editor_connect:
            assert args == []
        self._side_channels_manager = SideChannelManager(side_channels)
        self._communicator = SharedMemoryCommunicator(editor_connect, timeout_wait)

        # The process that is started. If None, no process was started
        self._proc1 = None
        if not editor_connect:
            self._proc1 = launch_executable(
                executable_name,
                args + ["--memory-path", str(self._communicator.communicator_id)],
            )
        else:
            logger.info(
                "Start training by pressing the Play button in the Unity Editor."
            )
        self._env_specs = self._communicator.generate_specs()
        self._communicator.give_unity_control()
        self._communicator.wait_for_unity()

    @property
    def behavior_specs(self) -> BehaviorMapping:
        return BehaviorMapping(self._env_specs)

    def reset(self) -> None:
        self._step(reset=True)

    @timed
    def step(self) -> None:
        self._step(reset=False)

    def query(self) -> None:
        """
        This will only send side channel data and get a response if any
        """
        channel_data = self._side_channels_manager.generate_side_channel_messages()
        self._communicator.write_side_channel_data(channel_data)
        self._communicator.give_unity_control(query=True)
        self._communicator.wait_for_unity()
        self._side_channels_manager.process_side_channel_message(
            self._communicator.read_and_clear_side_channel_data()
        )

    def _step(self, reset: bool = False) -> None:
        if not self._communicator.active:
            raise UnityCommunicationException("Communicator has stopped.")
        channel_data = self._side_channels_manager.generate_side_channel_messages()
        self._communicator.write_side_channel_data(channel_data)
        self._communicator.give_unity_control(reset)
        self._communicator.wait_for_unity()
        if not self._communicator.active:
            raise UnityCommunicationException("Communicator has stopped.")
        self._side_channels_manager.process_side_channel_message(
            self._communicator.read_and_clear_side_channel_data()
        )
        if len(self._env_specs) != self._communicator.num_behaviors:
            self._env_specs = self._communicator.generate_specs()

    def _assert_behavior_exists(self, behavior_name: BehaviorName) -> None:
        if behavior_name not in self._env_specs:
            raise UnityActionException(
                f"The behavior {behavior_name} does not correspond to one existing "
                f"in the environment"
            )

    def set_actions(self, behavior_name: BehaviorName, action: ActionTuple) -> None:
        self._assert_behavior_exists(behavior_name)
        expected_n_agents = self._communicator.get_n_decisions_requested(behavior_name)
        if expected_n_agents == 0 and len(action) != 0:
            raise UnityActionException(
                f"The behavior {behavior_name} does not need an input this step"
            )
        spec = self._env_specs[behavior_name]

        # continuous
        if action.continuous is not None:
            expected_cont_shape = (expected_n_agents, spec.action_spec.continuous_size)
            if action.continuous.shape != expected_cont_shape:
                raise UnityActionException(
                    f"The behavior {behavior_name} needs a continuous input of "
                    f"dimension {expected_cont_shape} but received input of "
                    f"dimension {action.continuous.shape}"
                )
            if action.continuous.dtype != np.float32:
                action.continuous = action.continuous.astype(np.float32)

        # discrete
        if action.discrete is not None:
            expected_disc_shape = (
                expected_n_agents,
                len(spec.action_spec.discrete_branches),
            )
            if action.discrete.shape != expected_disc_shape:
                raise UnityActionException(
                    f"The behavior {behavior_name} needs a discrete input of "
                    f"dimension {expected_disc_shape} but received input of "
                    f"dimension {action.discrete.shape}"
                )
            if action.discrete.dtype != np.int32:
                action.discrete = action.discrete.astype(np.int32)

        self._communicator.set_actions(behavior_name, action)

    def set_action_for_agent(
        self, behavior_name: BehaviorName, agent_id: int, action: ActionTuple
    ) -> None:
        raise NotImplementedError("Method not implemented.")

    def get_steps(
        self, behavior_name: BehaviorName
    ) -> Tuple[DecisionSteps, TerminalSteps]:
        self._assert_behavior_exists(behavior_name)
        return self._communicator.get_steps(behavior_name)

    def close(self):
        """
        Sends a shutdown signal to the unity environment, and closes the communication.
        """
        self._communicator.close()
        if self._proc1 is not None:
            # Wait a bit for the process to shutdown, but kill it if it takes too long
            try:
                self._proc1.wait(timeout=5)
                signal_name = None  # returncode_to_signal_name(self.proc1.returncode)
                signal_name = f" ({signal_name})" if signal_name else ""
                return_info = (
                    f"Environment shut down with return"
                    f"code{self._proc1.returncode}{signal_name}."
                )
                logger.info(return_info)
            except subprocess.TimeoutExpired:
                logger.info("Environment timed out shutting down. Killing...")
                self._proc1.kill()
            # Set to None so we don't try to close multiple times.
            self._proc1 = None
