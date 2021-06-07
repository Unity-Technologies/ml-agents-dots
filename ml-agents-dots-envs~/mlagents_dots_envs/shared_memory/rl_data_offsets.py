import numpy as np

from typing import Tuple, Optional, NamedTuple, List
from mlagents_dots_envs.shared_memory.base_shared_memory import BaseSharedMemory

from mlagents_envs.base_env import (
    BehaviorSpec,
    ObservationSpec,
    DimensionProperty,
    ObservationType,
    ActionSpec,
)


class RLDataOffsets(NamedTuple):
    """
    Contains the offsets to the data for a section of the RL data
    """

    # data
    name: str
    max_n_agents: int
    behavior_spec: BehaviorSpec

    # offsets: decision steps
    decision_n_agents_offset: int
    decision_obs_offset: Tuple[int, ...]
    decision_rewards_offset: int
    decision_agent_id_offset: int
    masks_offset: Optional[int]

    # offsets: termination steps
    termination_n_agents_offset: int
    termination_obs_offset: Tuple[int, ...]
    termination_reward_offset: int
    termination_status_offset: int
    termination_agent_id_offset: int

    # offsets : actions
    continuous_action_offset: int
    discrete_action_offset: int

    @staticmethod
    def from_mem(mem: BaseSharedMemory, offset: int) -> Tuple["RLDataOffsets", int]:
        # Generates the offsets
        # string : behavior name
        # int : 4 bytes : maximum number of Agents

        # int: number_observations
        # for each observation :
        #     3 int : shape
        #     3 int : dimension property
        #     1 int : observation type
        # int: number of continuous actions
        # int: number discrete branches
        # for each discrete branch :
        #     1 int : number of discrete actions in branch

        # end of specs

        # 4 bytes : n_agents at current step
        # ? Bytes : the data : obs,reward,done,max_step,agent_id,masks,action

        # Get the specs of the group
        name, offset = mem.get_string(offset)
        max_n_agents, offset = mem.get_int(offset)

        n_obs, offset = mem.get_int(offset)
        obs_specs: List[ObservationSpec] = []
        for i in range(n_obs):
            shape: Tuple[int, ...] = ()
            for _ in range(3):
                s, offset = mem.get_int(offset)
                if s != 0:
                    shape += (s,)
            dim_prop: Tuple[DimensionProperty] = ()
            for _ in range(3):
                dp, offset = mem.get_int(offset)
                if len(dim_prop) < len(shape):
                    dim_prop += (DimensionProperty(dp),)
            ot, offset = mem.get_int(offset)
            obs_type = ObservationType(ot)
            obs_specs.append(ObservationSpec(shape, dim_prop, obs_type, f"obs_{i}"))

        n_c_action, offset = mem.get_int(offset)
        n_d_action, offset = mem.get_int(offset)
        d_action_branches: Tuple[int, ...] = ()
        for _ in range(n_d_action):
            a_size, offset = mem.get_int(offset)
            d_action_branches += (a_size,)
        act_specs = ActionSpec(n_c_action, d_action_branches)
        behavior_spec = BehaviorSpec(obs_specs, act_specs)

        #  Compute the offsets for decision steps
        # n_agents
        decision_n_agents_offset = offset
        _, offset = mem.get_int(offset)
        # observations
        decision_obs_offset: Tuple[int, ...] = ()
        for spec in obs_specs:
            decision_obs_offset += (offset,)
            offset += 4 * max_n_agents * np.prod(spec.shape)
        # rewards
        decision_rewards_offset = offset
        offset += 4 * max_n_agents
        # agent id
        decision_agent_id_offset = offset
        offset += 4 * max_n_agents
        # mask
        if act_specs.discrete_size == 0:
            mask_offset = None
        else:
            mask_offset = offset
            offset += max_n_agents * int(np.sum(act_specs.discrete_branches))

        #  Compute the offsets for termination steps
        # n_agents
        termination_n_agents_offset = offset
        _, offset = mem.get_int(offset)
        # observations
        termination_obs_offset: Tuple[int, ...] = ()
        for spec in obs_specs:
            termination_obs_offset += (offset,)
            offset += 4 * max_n_agents * np.prod(np.prod(spec.shape))
        # rewards
        termination_reward_offset = offset
        offset += 4 * max_n_agents
        # status
        termination_status_offset = offset
        offset += max_n_agents
        # agent id
        termination_agent_id_offset = offset
        offset += 4 * max_n_agents

        #  Compute the offsets for actions
        c_act_offset = offset
        offset += 4 * max_n_agents * act_specs.continuous_size
        d_act_offset = offset
        offset += 4 * max_n_agents * len(act_specs.discrete_branches)

        # Create the object
        result = RLDataOffsets(
            name=name,
            max_n_agents=max_n_agents,
            behavior_spec=behavior_spec,
            # decision steps
            decision_n_agents_offset=decision_n_agents_offset,
            decision_obs_offset=decision_obs_offset,
            decision_rewards_offset=decision_rewards_offset,
            decision_agent_id_offset=decision_agent_id_offset,
            masks_offset=mask_offset,
            # termination steps
            termination_n_agents_offset=termination_n_agents_offset,
            termination_obs_offset=termination_obs_offset,
            termination_reward_offset=termination_reward_offset,
            termination_status_offset=termination_status_offset,
            termination_agent_id_offset=termination_agent_id_offset,
            # actions
            continuous_action_offset=c_act_offset,
            discrete_action_offset=d_act_offset,
        )
        return result, offset
