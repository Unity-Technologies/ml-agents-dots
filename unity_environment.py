import atexit
import glob
import logging
import numpy as np
import os
import subprocess
from typing import Dict, List, Optional, Any

from mlagents.envs.side_channel.side_channel import SideChannel

from mlagents.envs.base_env import BaseEnv, BatchedStepResult, AgentGroupSpec
from mlagents.envs.timers import timed, hierarchical_timer
from mlagents.envs.exception import (
    UnityEnvironmentException,
    UnityCommunicationException,
    UnityActionException,
    UnityTimeOutException,
)

from sys import platform
import signal
import struct

from .shared_memory_communicator import SharedMemoryCom

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mlagents.envs")


class UnityEnvironment(BaseEnv):
    def __init__(
        self,
        executable_name: Optional[str] = None,
        args: Optional[List[str]] = None,
        side_channels: Optional[List[SideChannel]] = None,
    ):
        """
        Starts a new unity environment and establishes a connection with the environment.

        :string executable_name: Name of Unity environment binary. If None, will try to connect to the Editor.
        :list args: Addition Unity command line arguments
        :list side_channels: Additional side channel for not-rl communication with Unity
        """
        args = args or []
        atexit.register(self.close)

        if executable_name is None:
            assert args == []
        self.side_channels = self.get_side_channels(side_channels)
        self.communicator = SharedMemoryCom(executable_name is None)

        # The process that is started. If None, no process was started
        self.proc1 = None
        if executable_name is not None:
            self.proc1 = self.executable_launcher(
                executable_name, self.communicator.get_file_name(), args
            )
        else:
            logger.info(
                "Start training by pressing the Play button in the Unity Editor."
            )

        self._env_state: Dict[str, BatchedStepResult] = {}
        self._env_specs: Dict[str, AgentGroupSpec] = {}
        self._env_actions: Dict[str, np.array] = {}
        self._is_first_message = True
        # self._update_group_specs(aca_output)

    @staticmethod
    def get_side_channels(
        side_c: Optional[List[SideChannel]]
    ) -> Dict[int, SideChannel]:
        side_channels_dict: Dict[int, SideChannel] = {}
        if side_c is not None:
            for _sc in side_c:
                if _sc.channel_type in side_channels_dict:
                    raise UnityEnvironmentException(
                        "There cannot be two side channels with the same channel type {0}.".format(
                            _sc.channel_type
                        )
                    )
                side_channels_dict[_sc.channel_type] = _sc

    @staticmethod
    def executable_launcher(exec_name, memory_path, args):
        cwd = os.getcwd()
        exec_name = (
            exec_name.strip()
            .replace(".app", "")
            .replace(".exe", "")
            .replace(".x86_64", "")
            .replace(".x86", "")
        )
        true_filename = os.path.basename(os.path.normpath(exec_name))
        logger.debug("The true file name is {}".format(true_filename))
        launch_string = None
        if platform == "linux" or platform == "linux2":
            candidates = glob.glob(os.path.join(cwd, exec_name) + ".x86_64")
            if len(candidates) == 0:
                candidates = glob.glob(os.path.join(cwd, exec_name) + ".x86")
            if len(candidates) == 0:
                candidates = glob.glob(exec_name + ".x86_64")
            if len(candidates) == 0:
                candidates = glob.glob(exec_name + ".x86")
            if len(candidates) > 0:
                launch_string = candidates[0]

        elif platform == "darwin":
            candidates = glob.glob(
                os.path.join(
                    cwd, exec_name + ".app", "Contents", "MacOS", true_filename
                )
            )
            if len(candidates) == 0:
                candidates = glob.glob(
                    os.path.join(exec_name + ".app", "Contents", "MacOS", true_filename)
                )
            if len(candidates) == 0:
                candidates = glob.glob(
                    os.path.join(cwd, exec_name + ".app", "Contents", "MacOS", "*")
                )
            if len(candidates) == 0:
                candidates = glob.glob(
                    os.path.join(exec_name + ".app", "Contents", "MacOS", "*")
                )
            if len(candidates) > 0:
                launch_string = candidates[0]
        elif platform == "win32":
            candidates = glob.glob(os.path.join(cwd, exec_name + ".exe"))
            if len(candidates) == 0:
                candidates = glob.glob(exec_name + ".exe")
            if len(candidates) > 0:
                launch_string = candidates[0]
        if launch_string is None:
            raise UnityEnvironmentException(
                "Couldn't launch the {0} environment. "
                "Provided filename does not match any environments.".format(
                    true_filename
                )
            )
        else:
            logger.debug("This is the launch string {}".format(launch_string))
            # Launch Unity environment
            args += ["--memory-path", str(memory_path)]
            try:
                return subprocess.Popen(
                    args,
                    # start_new_session=True means that signals to the parent python process
                    # (e.g. SIGINT from keyboard interrupt) will not be sent to the new process on POSIX platforms.
                    # This is generally good since we want the environment to have a chance to shutdown,
                    # but may be undesirable in come cases; if so, we'll add a command-line toggle.
                    # Note that on Windows, the CTRL_C signal will still be sent.
                    start_new_session=True,
                )
            except PermissionError as perm:
                # This is likely due to missing read or execute permissions on file.
                raise UnityEnvironmentException(
                    f"Error when trying to launch environment - make sure "
                    f"permissions are set correctly. For example "
                    f'"chmod -R 755 {launch_string}"'
                ) from perm

    def reset(self) -> None:
        outputs = self.communicator.exchange(self._generate_reset_input())
        if outputs is None:
            raise UnityCommunicationException("Communicator has stopped.")
        self._update_group_specs(outputs)
        rl_output = outputs.rl_output
        self._update_state(rl_output)
        self._is_first_message = False
        self._env_actions.clear()

    @timed
    def step(self) -> None:
        if self._is_first_message:
            return self.reset()
        # fill the blanks for missing actions
        for group_name in self._env_specs:
            if group_name not in self._env_actions:
                n_agents = 0
                if group_name in self._env_state:
                    n_agents = self._env_state[group_name].n_agents()
                self._env_actions[group_name] = self._env_specs[
                    group_name
                ].create_empty_action(n_agents)
        step_input = self._generate_step_input(self._env_actions)
        with hierarchical_timer("communicator.exchange"):
            outputs = self.communicator.exchange(step_input)
        if outputs is None:
            raise UnityCommunicationException("Communicator has stopped.")
        self._update_group_specs(outputs)
        rl_output = outputs.rl_output
        self._update_state(rl_output)
        self._env_actions.clear()

    def get_agent_groups(self) -> List[str]:
        return list(self._env_specs.keys())

    def _assert_group_exists(self, agent_group: str) -> None:
        if agent_group not in self._env_specs:
            raise UnityActionException(
                "The group {0} does not correspond to an existing agent group "
                "in the environment".format(agent_group)
            )

    def set_actions(self, agent_group: str, action: np.array) -> None:
        self._assert_group_exists(agent_group)
        if agent_group not in self._env_state:
            return
        spec = self._env_specs[agent_group]
        expected_type = np.float32 if spec.is_action_continuous() else np.int32
        expected_shape = (self._env_state[agent_group].n_agents(), spec.action_size)
        if action.shape != expected_shape:
            raise UnityActionException(
                "The group {0} needs an input of dimension {1} but received input of dimension {2}".format(
                    agent_group, expected_shape, action.shape
                )
            )
        if action.dtype != expected_type:
            action = action.astype(expected_type)
        self._env_actions[agent_group] = action

    def set_action_for_agent(
        self, agent_group: str, agent_id: int, action: np.array
    ) -> None:
        self._assert_group_exists(agent_group)
        if agent_group not in self._env_state:
            return
        spec = self._env_specs[agent_group]
        expected_shape = (spec.action_size,)
        if action.shape != expected_shape:
            raise UnityActionException(
                "The Agent {0} in group {1} needs an input of dimension {2} but received input of dimension {3}".format(
                    agent_id, agent_group, expected_shape, action.shape
                )
            )
        expected_type = np.float32 if spec.is_action_continuous() else np.int32
        if action.dtype != expected_type:
            action = action.astype(expected_type)

        if agent_group not in self._env_actions:
            self._env_actions[agent_group] = self._empty_action(
                spec, self._env_state[agent_group].n_agents()
            )
        try:
            index = np.where(self._env_state[agent_group].agent_id == agent_id)[0][0]
        except IndexError as ie:
            raise IndexError(
                "agent_id {} is did not request a decision at the previous step".format(
                    agent_id
                )
            ) from ie
        self._env_actions[agent_group][index] = action

    def get_step_result(self, agent_group: str) -> BatchedStepResult:
        self._assert_group_exists(agent_group)
        return self._env_state[agent_group]

    def get_agent_group_spec(self, agent_group: str) -> AgentGroupSpec:
        self._assert_group_exists(agent_group)
        return self._env_specs[agent_group]

    def close(self):
        """
        Sends a shutdown signal to the unity environment, and closes the socket connection.
        """
        self.communicator.close()
        if self.proc1 is not None:
            # Wait a bit for the process to shutdown, but kill it if it takes too long
            try:
                self.proc1.wait(timeout=5)
                signal_name = self.returncode_to_signal_name(self.proc1.returncode)
                signal_name = f" ({signal_name})" if signal_name else ""
                return_info = f"Environment shut down with return code {self.proc1.returncode}{signal_name}."
                logger.info(return_info)
            except subprocess.TimeoutExpired:
                logger.info("Environment timed out shutting down. Killing...")
                self.proc1.kill()
            # Set to None so we don't try to close multiple times.
            self.proc1 = None

    @staticmethod
    def _parse_side_channel_message(
        side_channels: Dict[int, SideChannel], data: bytearray
    ) -> None:
        offset = 0
        while offset < len(data):
            try:
                channel_type, message_len = struct.unpack_from("<ii", data, offset)
                offset = offset + 8
                message_data = data[offset : offset + message_len]
                offset = offset + message_len
            except Exception:
                raise UnityEnvironmentException(
                    "There was a problem reading a message in a SideChannel. "
                    "Please make sure the version of MLAgents in Unity is "
                    "compatible with the Python version."
                )
            if len(message_data) != message_len:
                raise UnityEnvironmentException(
                    "The message received by the side channel {0} was "
                    "unexpectedly short. Make sure your Unity Environment "
                    "sending side channel data properly.".format(channel_type)
                )
            if channel_type in side_channels:
                side_channels[channel_type].on_message_received(message_data)
            else:
                logger.warning(
                    "Unknown side channel data received. Channel type "
                    ": {0}.".format(channel_type)
                )

    @staticmethod
    def _generate_side_channel_data(side_channels: Dict[int, SideChannel]) -> bytearray:
        result = bytearray()
        for channel_type, channel in side_channels.items():
            for message in channel.message_queue:
                result += struct.pack("<ii", channel_type, len(message))
                result += message
            channel.message_queue = []
        return result

    @staticmethod
    def returncode_to_signal_name(returncode: int) -> Optional[str]:
        """
        Try to convert return codes into their corresponding signal name.
        E.g. returncode_to_signal_name(-2) -> "SIGINT"
        """
        try:
            # A negative value -N indicates that the child was terminated by signal N (POSIX only).
            s = signal.Signals(-returncode)  # pylint: disable=no-member
            return s.name
        except Exception:
            # Should generally be a ValueError, but catch everything just in case.
            return None
