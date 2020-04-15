import uuid
import struct
import signal
import os
import glob
import subprocess
from sys import platform
from mlagents_envs.exception import UnityEnvironmentException
from mlagents_envs.side_channel.side_channel import SideChannel
from typing import Dict, List, Optional
from mlagents_envs.logging_util import get_logger

logger = get_logger(__name__)


def validate_environment_path(env_path: str) -> Optional[str]:
    # Strip out executable extensions if passed
    env_path = (
        env_path.strip()
        .replace(".app", "")
        .replace(".exe", "")
        .replace(".x86_64", "")
        .replace(".x86", "")
    )
    true_filename = os.path.basename(os.path.normpath(env_path))
    logger.debug("The true file name is {}".format(true_filename))

    if not (glob.glob(env_path) or glob.glob(env_path + ".*")):
        return None

    cwd = os.getcwd()
    launch_string = None
    true_filename = os.path.basename(os.path.normpath(env_path))
    if platform == "linux" or platform == "linux2":
        candidates = glob.glob(os.path.join(cwd, env_path) + ".x86_64")
        if len(candidates) == 0:
            candidates = glob.glob(os.path.join(cwd, env_path) + ".x86")
        if len(candidates) == 0:
            candidates = glob.glob(env_path + ".x86_64")
        if len(candidates) == 0:
            candidates = glob.glob(env_path + ".x86")
        if len(candidates) > 0:
            launch_string = candidates[0]

    elif platform == "darwin":
        candidates = glob.glob(
            os.path.join(cwd, env_path + ".app", "Contents", "MacOS", true_filename)
        )
        if len(candidates) == 0:
            candidates = glob.glob(
                os.path.join(env_path + ".app", "Contents", "MacOS", true_filename)
            )
        if len(candidates) == 0:
            candidates = glob.glob(
                os.path.join(cwd, env_path + ".app", "Contents", "MacOS", "*")
            )
        if len(candidates) == 0:
            candidates = glob.glob(
                os.path.join(env_path + ".app", "Contents", "MacOS", "*")
            )
        if len(candidates) > 0:
            launch_string = candidates[0]
    elif platform == "win32":
        candidates = glob.glob(os.path.join(cwd, env_path + ".exe"))
        if len(candidates) == 0:
            candidates = glob.glob(env_path + ".exe")
        if len(candidates) > 0:
            launch_string = candidates[0]
    return launch_string
    # TODO: END REMOVE


def get_side_channels(
    side_c: Optional[List[SideChannel]]
) -> Dict[uuid.UUID, SideChannel]:
    side_channels_dict: Dict[uuid.UUID, SideChannel] = {}
    if side_c is not None:
        for _sc in side_c:
            if _sc.channel_id in side_channels_dict:
                raise UnityEnvironmentException(
                    f"There cannot be two side channels with "
                    f"the same channel id {_sc.channel_id}."
                )
            side_channels_dict[_sc.channel_id] = _sc
    return side_channels_dict


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
            os.path.join(cwd, exec_name + ".app", "Contents", "MacOS", true_filename)
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
            "Provided filename does not match any environments.".format(true_filename)
        )
    else:
        logger.debug("This is the launch string {}".format(launch_string))
        # Launch Unity environment
        subprocess_args = [launch_string]
        subprocess_args += ["--memory-path", str(memory_path)]
        subprocess_args += args
        try:
            return subprocess.Popen(
                subprocess_args,
                # start_new_session=True means that signals to the parent python process
                # (e.g. SIGINT from keyboard interrupt) will not be sent to the new
                # process on POSIX platforms. This is generally good since we want the
                # environment to have a chance to shutdown, but may be undesirable in
                # some cases; if so, we'll add a command-line toggle.
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


def parse_side_channel_message(
    side_channels: Dict[uuid.UUID, SideChannel], data: bytearray
) -> None:
    offset = 0
    while offset < len(data):
        try:
            channel_id = uuid.UUID(bytes_le=bytes(data[offset : offset + 16]))
            offset += 16
            message_len, = struct.unpack_from("<i", data, offset)
            offset = offset + 4
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
                "sending side channel data properly.".format(channel_id)
            )
        if channel_id in side_channels:
            side_channels[channel_id].on_message_received(message_data)
        else:
            logger.warning(
                "Unknown side channel data received. Channel type "
                ": {0}.".format(channel_id)
            )


def generate_side_channel_data(
    side_channels: Dict[uuid.UUID, SideChannel]
) -> bytearray:
    result = bytearray()
    for channel_id, channel in side_channels.items():
        for message in channel.message_queue:
            result += channel_id.bytes_le
            result += struct.pack("<i", len(message))
            result += message
        channel.message_queue = []
    return result


def returncode_to_signal_name(returncode: int) -> Optional[str]:
    """
    Try to convert return codes into their corresponding signal name.
    E.g. returncode_to_signal_name(-2) -> "SIGINT"
    """
    try:
        # A negative value -N indicates that the child was terminated by
        # signal N (POSIX only).
        s = signal.Signals(-returncode)  # pylint: disable=no-member
        return s.name
    except Exception:
        # Should generally be a ValueError, but catch everything just in case.
        return None


# def create_behavior_spec(
#     offsets: Dict[str, BehaviorFileOffsets]
# ) -> Dict[str, BehaviorSpec]:
#     result: Dict[str, BehaviorSpec] = {}
#     for k, v in offsets.items():
#         action_type = (
#             ActionType.CONTINUOUS if v.is_continuous else ActionType.DISCRETE
#         )
#         action_shape = v.a_size
#         if action_type == ActionType.DISCRETE:
#             action_shape = v.discrete_branches
#         result[k] = BehaviorSpec(v.obs_shapes, action_type, action_shape)
#     return result
