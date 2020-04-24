import numpy as np

from typing import Tuple, Optional, NamedTuple, List
from mlagents_dots_envs.shared_memory.base_shared_memory import BaseSharedMemory


class RLDataOffsets(NamedTuple):
    """
    Contains the offsets to the data for a section of the RL data
    """

    # data
    name: str
    max_n_agents: int
    is_action_continuous: bool
    action_size: int
    discrete_branches: Optional[Tuple[int, ...]]
    obs_shapes: List[Tuple[int, ...]]

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
    action_offset: int

    @staticmethod
    def from_mem(mem: BaseSharedMemory, offset: int) -> Tuple["RLDataOffsets", int]:
        # Generates the offsets
        # string : behavior name
        # int : 4 bytes : maximum number of Agents
        # bool : 1 byte : is action discrete (False) or continuous (True)
        # int : 4 bytes : action space size (continuous) / number of branches (discrete)
        # -- If discrete only : array of action sizes for each branch
        # int : 4 bytes : number of observations
        # For each observation :
        # 3 int : shape (the shape of the tensor observation for one agent
        # start of the section that will change every step
        # 4 bytes : n_agents at current step
        # ? Bytes : the data : obs,reward,done,max_step,agent_id,masks,action

        # Get the specs of the group
        name, offset = mem.get_string(offset)
        max_n_agents, offset = mem.get_int(offset)
        is_continuous, offset = mem.get_bool(offset)
        action_size, offset = mem.get_int(offset)
        discrete_branches = None
        if not is_continuous:
            discrete_branches = ()  # type: ignore
            for _ in range(action_size):
                branch_size, offset = mem.get_int(offset)
                discrete_branches += (branch_size,)  # type: ignore
        n_obs, offset = mem.get_int(offset)
        obs_shapes: List[Tuple[int, ...]] = []
        for _ in range(n_obs):
            shape = ()  # type: ignore
            for _ in range(3):
                s, offset = mem.get_int(offset)
                if s != 0:
                    shape += (s,)  # type: ignore
            obs_shapes += [shape]

        #  Compute the offsets for decision steps
        # n_agents
        decision_n_agents_offset = offset
        _, offset = mem.get_int(offset)
        # observations
        decision_obs_offset: Tuple[int, ...] = ()
        for s in obs_shapes:
            decision_obs_offset += (offset,)
            offset += 4 * max_n_agents * np.prod(s)
        # rewards
        decision_rewards_offset = offset
        offset += 4 * max_n_agents
        # agent id
        decision_agent_id_offset = offset
        offset += 4 * max_n_agents
        # mask
        if is_continuous:
            mask_offset = None
        else:
            mask_offset = offset
            offset += max_n_agents * int(np.sum(discrete_branches))

        #  Compute the offsets for termination steps
        # n_agents
        termination_n_agents_offset = offset
        _, offset = mem.get_int(offset)
        # observations
        termination_obs_offset: Tuple[int, ...] = ()
        for s in obs_shapes:
            termination_obs_offset += (offset,)
            offset += 4 * max_n_agents * np.prod(s)
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
        act_offset = offset
        offset += 4 * max_n_agents * action_size

        # Create the object
        result = RLDataOffsets(
            name=name,
            max_n_agents=max_n_agents,
            is_action_continuous=is_continuous,
            action_size=action_size,
            discrete_branches=discrete_branches,
            obs_shapes=obs_shapes,
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
            action_offset=act_offset,
        )
        return result, offset
