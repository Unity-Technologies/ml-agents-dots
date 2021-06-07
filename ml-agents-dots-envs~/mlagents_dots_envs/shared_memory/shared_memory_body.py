import numpy as np
from mlagents_dots_envs.shared_memory.base_shared_memory import BaseSharedMemory
from mlagents_dots_envs.shared_memory.rl_data_offsets import RLDataOffsets
from typing import Dict, List
from mlagents_envs.base_env import (
    DecisionSteps,
    TerminalSteps,
    BehaviorSpec,
    ActionTuple,
)


class SharedMemoryBody(BaseSharedMemory):
    def __init__(
        self,
        file_name: str,
        create_file: bool = False,
        copy_from: "SharedMemoryBody" = None,
        side_channel_buffer_size: int = 0,
        rl_data_buffer_size: int = 0,
    ):
        self._offset_dict: Dict[str, RLDataOffsets] = {}
        if create_file and copy_from is None:
            size = side_channel_buffer_size + rl_data_buffer_size
            super(SharedMemoryBody, self).__init__(file_name, True, size)
            self._side_channel_buffer_size = side_channel_buffer_size
            self._rl_data_buffer_size = rl_data_buffer_size
            return
        if create_file and copy_from is not None:
            # can only increase the size of the file
            size = side_channel_buffer_size + rl_data_buffer_size
            super(SharedMemoryBody, self).__init__(file_name, True, size)
            assert side_channel_buffer_size >= copy_from._side_channel_buffer_size
            assert rl_data_buffer_size >= copy_from._rl_data_buffer_size
            self._side_channel_buffer_size = side_channel_buffer_size
            self._rl_data_buffer_size = rl_data_buffer_size
            self.side_channel_data = copy_from.side_channel_data
            self.rl_data = copy_from.rl_data
        if not create_file:
            size = side_channel_buffer_size + rl_data_buffer_size
            super(SharedMemoryBody, self).__init__(file_name, False, size)
            self._side_channel_buffer_size = side_channel_buffer_size
            self._rl_data_buffer_size = rl_data_buffer_size
            self._refresh_offsets()

    def _refresh_offsets(self):
        self._offset_dict.clear()
        offset = self.rl_data_offset
        while offset < self.rl_data_offset + self._rl_data_buffer_size:
            data_offsets, offset = RLDataOffsets.from_mem(self, offset)
            self._offset_dict[data_offsets.name] = data_offsets

    @property
    def side_channel_data(self) -> bytearray:
        offset = 0
        len_data, offset = self.get_int(offset)
        return self.accessor[offset : offset + len_data]

    @side_channel_data.setter
    def side_channel_data(self, data: bytearray) -> None:
        if len(data) > self._side_channel_buffer_size - 4:
            raise Exception("TODO")
        offset = 0
        offset = self.set_int(offset, len(data))
        self.accessor[offset : offset + len(data)] = data

    @property
    def rl_data_offset(self):
        return self._side_channel_buffer_size

    @property
    def rl_data(self) -> bytearray:
        offset = self.rl_data_offset
        size = self._rl_data_buffer_size
        return self.accessor[offset : offset + size]

    @rl_data.setter
    def rl_data(self, data: bytearray) -> None:
        if len(data) > self._rl_data_buffer_size:
            raise Exception("TODO")
        offset = self.rl_data_offset
        size = self._rl_data_buffer_size
        self.accessor[offset : offset + size] = data
        self._refresh_offsets()

    def get_decision_steps(self, key: str) -> DecisionSteps:
        assert key in self._offset_dict
        offsets = self._offset_dict[key]
        n_agents, _ = self.get_int(offsets.decision_n_agents_offset)
        obs: List[np.ndarray] = []
        for obs_offset, obs_spec in zip(
            offsets.decision_obs_offset, offsets.behavior_spec.observation_specs
        ):
            obs_shape = (n_agents,) + obs_spec.shape
            arr = self.get_ndarray(obs_offset, obs_shape, np.float32)
            obs.append(arr)
        return DecisionSteps(
            obs=obs,
            reward=self.get_ndarray(
                offsets.decision_rewards_offset, (n_agents), np.float32
            ),
            agent_id=self.get_ndarray(
                offsets.decision_agent_id_offset, (n_agents), np.int32
            ),
            action_mask=self._generate_action_masks(offsets, n_agents),
            # TODO: Communicate these values
            group_id=np.zeros((n_agents), dtype=np.int32),
            group_reward=np.zeros((n_agents), dtype=np.float32),
        )

    def get_terminal_steps(self, key: str) -> TerminalSteps:
        assert key in self._offset_dict
        offsets = self._offset_dict[key]
        n_agents, _ = self.get_int(offsets.termination_n_agents_offset)
        obs: List[np.ndarray] = []
        for obs_offset, obs_spec in zip(
            offsets.termination_obs_offset, offsets.behavior_spec.observation_specs
        ):
            obs_shape = (n_agents,) + obs_spec.shape
            arr = self.get_ndarray(obs_offset, obs_shape, np.float32)
            obs.append(arr)
        return TerminalSteps(
            obs=obs,
            reward=self.get_ndarray(
                offsets.termination_reward_offset, (n_agents), np.float32
            ),
            agent_id=self.get_ndarray(
                offsets.termination_agent_id_offset, (n_agents), np.int32
            ),
            interrupted=self.get_ndarray(
                offsets.termination_status_offset, (n_agents), np.bool
            ),
            # TODO: Communicate these values
            group_id=np.zeros((n_agents), dtype=np.int32),
            group_reward=np.zeros((n_agents), dtype=np.float32),
        )

    def set_actions(self, key: str, data: ActionTuple) -> None:
        assert key in self._offset_dict
        offsets = self._offset_dict[key]
        if data.continuous is not None:
            self.set_ndarray(offsets.continuous_action_offset, data.continuous)
        if data.discrete is not None:
            self.set_ndarray(offsets.discrete_action_offset, data.discrete)

    def get_n_decisions_requested(self, key: str) -> int:
        assert key in self._offset_dict
        offsets = self._offset_dict[key]
        result, _ = self.get_int(offsets.decision_n_agents_offset)
        return result

    @property
    def num_behaviors(self) -> int:
        return len(self._offset_dict)

    def generate_specs(self) -> Dict[str, BehaviorSpec]:
        result: Dict[str, BehaviorSpec] = {}
        for key in self._offset_dict:
            offsets = self._offset_dict[key]
            result[key] = offsets.behavior_spec
        return result

    def _generate_action_masks(
        self, offsets: RLDataOffsets, n_agents: int
    ) -> np.ndarray:
        start = offsets.masks_offset
        if start is None:
            return None
        branches = offsets.behavior_spec.action_spec.discrete_branches
        result: List[np.ndarray] = []
        for branch_size in branches:
            result += [self.get_ndarray(start, (n_agents, branch_size), np.bool)]
            start += offsets.max_n_agents * branch_size
        return result
