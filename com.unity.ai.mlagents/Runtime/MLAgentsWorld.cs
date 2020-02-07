using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;

namespace Unity.AI.MLAgents
{
    public enum ActionType : int
    {
        DISCRETE = 0,
        CONTINUOUS = 1,
    }

    public struct MLAgentsWorld : IDisposable
    {
        [ReadOnly] internal NativeArray<int3> SensorShapes;
        [ReadOnly] internal int ActionSize;
        [ReadOnly] internal ActionType ActionType;
        [ReadOnly] internal NativeArray<int> DiscreteActionBranches;

        [ReadOnly] internal NativeArray<int> ObservationOffsets;

        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<float> Sensors;
        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<float> Rewards;
        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<bool> DoneFlags;
        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<bool> MaxStepFlags;
        [NativeDisableParallelForRestriction] internal NativeArray<Entity> AgentEntityIds;
        [NativeDisableParallelForRestriction] internal NativeArray<int> AgentIds;
        [NativeDisableParallelForRestriction] internal NativeArray<bool> ActionMasks;

        //https://forum.unity.com/threads/is-it-okay-to-read-a-nativecounter-concurrents-value-in-a-parallel-job.533037/
        [NativeDisableParallelForRestriction] internal NativeCounter AgentCounter;
        [NativeDisableParallelForRestriction] internal NativeCounter ActionCounter;

        [NativeDisableParallelForRestriction] internal NativeArray<float> ContinuousActuators;
        [NativeDisableParallelForRestriction] internal NativeArray<int> DiscreteActuators;
        [NativeDisableParallelForRestriction] internal NativeArray<Entity> ActionAgentIds; // Keep track of the Ids for the next action step
        [NativeDisableParallelForRestriction] internal NativeArray<bool> ActionDoneFlags; // Keep track of the Done flags for the next action step


        /// <summary>
        /// Creates a data container used to write data needed for decisions and retrieve action data.
        /// </summary>
        /// <param name="maximumNumberAgents"> The maximum number of decisions that can be requested between each MLAgentsSystem update </param>
        /// <param name="actionType"> An ActionType enum (DISCRETE / CONTINUOUS) specifying the type of actions the MLAgentsWorld will produce </param>
        /// <param name="obsShapes"> An array of int3 corresponding to the shape of the expected observations (one int3 per observation) </param>
        /// <param name="actionSize"> The number of actions the MLAgentsWorld is expected to generate for each decision.
        ///  - If CONTINUOUS ActionType : The number of floats the action contains
        ///  - If DISCRETE ActionType : The number of branches (integer actions) the action contains </param>
        /// <param name="discreteActionBranches"> For DISCRETE ActionType only : an array of int specifying the number of possible int values each
        /// action branch has. (Must be of the same length as actionSize </param>
        public MLAgentsWorld(
            int maximumNumberAgents,
            ActionType actionType,
            int3[] obsShapes,
            int actionSize,
            int[] discreteActionBranches = null)
        {
            SensorShapes = new NativeArray<int3>(obsShapes, Allocator.Persistent);
            ActionSize = actionSize;
            ActionType = actionType;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (ActionType == ActionType.DISCRETE)
            {
                if (discreteActionBranches == null)
                {
                    throw new MLAgentsException("For Discrete control, the number of possible actions for each branch must be specified.");
                }
                if (discreteActionBranches.Length != actionSize)
                {
                    throw new MLAgentsException("For Discrete control, the number of branches must be equal to the action size.");
                }
            }
#endif
            if (discreteActionBranches == null)
            {
                discreteActionBranches = new int[0];
            }
            DiscreteActionBranches = new NativeArray<int>(discreteActionBranches, Allocator.Persistent);

            ObservationOffsets = new NativeArray<int>(SensorShapes.Length, Allocator.Persistent);
            int currentOffset = 0;
            for (int i = 0; i < SensorShapes.Length; i++)
            {
                ObservationOffsets[i] = currentOffset;
                int3 s = SensorShapes[i];
                currentOffset += s.GetTotalTensorSize() * maximumNumberAgents;
            }

            Sensors = new NativeArray<float>(currentOffset, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Rewards = new NativeArray<float>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            DoneFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            MaxStepFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            AgentEntityIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            AgentIds = new NativeArray<int>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            int nMasks = 0;
            if (ActionType == ActionType.DISCRETE)
            {
                nMasks = DiscreteActionBranches.Sum();
            }
            ActionMasks = new NativeArray<bool>(maximumNumberAgents * nMasks, Allocator.Persistent);

            int daSize = 0;
            int caSize = 0;
            if (ActionType == ActionType.DISCRETE)
            {
                daSize = ActionSize;
            }
            else
            {
                caSize = ActionSize;
            }

            AgentCounter = new NativeCounter(Allocator.Persistent);
            ActionCounter = new NativeCounter(Allocator.Persistent);

            ContinuousActuators = new NativeArray<float>(maximumNumberAgents * caSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            DiscreteActuators = new NativeArray<int>(maximumNumberAgents * daSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            ActionDoneFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ActionAgentIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        internal void ResetActionsCounter()
        {
            ActionCounter.Count = 0;
        }

        internal void ResetDecisionsCounter()
        {
            AgentCounter.Count = 0;
        }

        internal void SetActionReady()
        {
            int count = AgentCounter.Count;
            ActionCounter.Count = count;
            ActionDoneFlags.Slice(0, count).CopyFrom(DoneFlags.Slice(0, count));
            ActionAgentIds.Slice(0, count).CopyFrom(AgentEntityIds.Slice(0, count));
        }

        public void Dispose()
        {
            SensorShapes.Dispose();
            DiscreteActionBranches.Dispose();
            ObservationOffsets.Dispose();
            Sensors.Dispose();
            Rewards.Dispose();
            DoneFlags.Dispose();
            MaxStepFlags.Dispose();
            AgentEntityIds.Dispose();
            AgentIds.Dispose();
            ActionMasks.Dispose();
            AgentCounter.Dispose();
            ActionCounter.Dispose();
            ContinuousActuators.Dispose();
            DiscreteActuators.Dispose();
            ActionDoneFlags.Dispose();
            ActionAgentIds.Dispose();
        }

        /// <summary>
        /// Requests a decision for a specific Entity to the MLAgentsWorld. The DecisionRequest
        /// struct this method returns can be used to provide data necessary for the Agent to
        /// take a decision for the Entity.
        /// </summary>
        /// <param name="entity"> The Entity the decision is tied to. The Entity is used to track
        /// sequences of actions of an Agent.</param>
        /// <returns></returns>
        public DecisionRequest RequestDecision(Entity entity)
        {
            var index = AgentCounter.ToConcurrent().Increment() - 1;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index > AgentEntityIds.Length)
            {
                throw new MLAgentsException("Number of decisions requested exceeds the set maximum of " + AgentEntityIds.Length);
            }
#endif
            AgentEntityIds[index] = entity;
            AgentIds[index] = entity.Index;
            return new DecisionRequest(index, this);
        }
    }
}
