using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using System;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Policy is a data container on which the user requests decisions.
    /// </summary>
    public struct Policy : IDisposable
    {
        /// <summary>
        /// Indicates if the Policy has been instantiated
        /// </summary>
        /// <value> True if the Policy was instantiated, False otherwise</value>
        public bool IsCreated
        {
            get { return DecisionAgentIds.IsCreated;}
        }

        [ReadOnly] internal NativeArray<int3> SensorShapes;
        [ReadOnly] internal int ActionSize;
        [ReadOnly] internal ActionType ActionType;
        [ReadOnly] internal NativeArray<int> DiscreteActionBranches;

        [ReadOnly] internal NativeArray<int> ObservationOffsets;

        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<float> DecisionObs;
        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<float> DecisionRewards;
        [NativeDisableParallelForRestriction] internal NativeArray<int> DecisionAgentIds;
        [NativeDisableParallelForRestriction] internal NativeArray<bool> DecisionActionMasks;
        [NativeDisableParallelForRestriction] internal NativeArray<Entity> DecisionAgentEntityIds;

        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<float> TerminationObs;
        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<float> TerminationRewards;
        [NativeDisableParallelForRestriction] internal NativeArray<int> TerminationAgentIds;
        [NativeDisableParallelForRestriction][WriteOnly] internal NativeArray<bool> TerminationStatus;

        //https://forum.unity.com/threads/is-it-okay-to-read-a-nativecounter-concurrents-value-in-a-parallel-job.533037/
        [NativeDisableParallelForRestriction] internal Counter DecisionCounter;
        [NativeDisableParallelForRestriction] internal Counter TerminationCounter;
        [NativeDisableParallelForRestriction] internal Counter ActionCounter;

        [NativeDisableParallelForRestriction] internal NativeArray<float> ContinuousActuators;
        [NativeDisableParallelForRestriction] internal NativeArray<int> DiscreteActuators;
        [NativeDisableParallelForRestriction] internal NativeArray<Entity> ActionAgentEntityIds; // Keep track of the Ids for the next action step

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle       m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel          m_DisposeSentinel;
#endif

        /// <summary>
        /// Creates a data container used to write data needed for decisions and retrieve action data.
        /// </summary>
        /// <param name="maximumNumberAgents"> The maximum number of decisions that can be requested between each MLAgentsSystem update </param>
        /// <param name="obsShapes"> An array of int3 corresponding to the shape of the expected observations (one int3 per observation) </param>
        /// <param name="actionType"> An ActionType enum (DISCRETE / CONTINUOUS) specifying the type of actions the Policy will produce </param>
        /// <param name="actionSize"> The number of actions the Policy is expected to generate for each decision.
        ///  - If CONTINUOUS ActionType : The number of floats the action contains
        ///  - If DISCRETE ActionType : The number of branches (integer actions) the action contains </param>
        /// <param name="discreteActionBranches"> For DISCRETE ActionType only : an array of int specifying the number of possible int values each
        /// action branch has. (Must be of the same length as actionSize </param>
        public Policy(
            int maximumNumberAgents,
            int3[] obsShapes,
            ActionType actionType,
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

            DecisionObs = new NativeArray<float>(currentOffset, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            DecisionRewards = new NativeArray<float>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            DecisionAgentEntityIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            DecisionAgentIds = new NativeArray<int>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            int nMasks = 0;
            if (ActionType == ActionType.DISCRETE)
            {
                nMasks = DiscreteActionBranches.Sum();
            }
            DecisionActionMasks = new NativeArray<bool>(maximumNumberAgents * nMasks, Allocator.Persistent);
            DecisionCounter = new Counter(Allocator.Persistent);

            TerminationObs = new NativeArray<float>(currentOffset, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            TerminationRewards = new NativeArray<float>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            TerminationAgentIds = new NativeArray<int>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            TerminationStatus = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            TerminationCounter = new Counter(Allocator.Persistent);

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

            ActionCounter = new Counter(Allocator.Persistent);

            ContinuousActuators = new NativeArray<float>(maximumNumberAgents * caSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            DiscreteActuators = new NativeArray<int>(maximumNumberAgents * daSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            ActionAgentEntityIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, Allocator.Persistent);
#endif
        }

        internal void ResetActionsCounter()
        {
            ActionCounter.Count = 0;
        }

        internal void ResetDecisionsAndTerminationCounters()
        {
            DecisionCounter.Count = 0;
            TerminationCounter.Count = 0;
        }

        internal void SetActionReady()
        {
            int count = DecisionCounter.Count;
            ActionCounter.Count = count;
            ActionAgentEntityIds.Slice(0, count).CopyFrom(DecisionAgentEntityIds.Slice(0, count));
        }

        /// <summary>
        /// Dispose of the Policy.
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            SensorShapes.Dispose();
            DiscreteActionBranches.Dispose();
            ObservationOffsets.Dispose();

            DecisionObs.Dispose();
            DecisionRewards.Dispose();
            DecisionAgentEntityIds.Dispose();
            DecisionAgentIds.Dispose();
            DecisionActionMasks.Dispose();
            DecisionCounter.Dispose();

            TerminationObs.Dispose();
            TerminationRewards.Dispose();
            TerminationAgentIds.Dispose();
            TerminationStatus.Dispose();
            TerminationCounter.Dispose();

            ActionCounter.Dispose();
            ContinuousActuators.Dispose();
            DiscreteActuators.Dispose();
            ActionAgentEntityIds.Dispose();
        }

        /// <summary>
        /// Requests a decision for a specific Entity to the Policy. The DecisionRequest
        /// struct this method returns can be used to provide data necessary for the Agent to
        /// take a decision for the Entity.
        /// </summary>
        /// <param name="entity"> The Entity the decision is tied to. The Entity is used to track
        /// sequences of actions of an Agent.</param>
        /// <returns></returns>
        public DecisionRequest RequestDecision(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new MLAgentsException($"Invalid operation, cannot request a decision on a non-initialized Policy");
            }
#endif
            var index = DecisionCounter.Increment() - 1;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index > DecisionAgentIds.Length)
            {
                throw new MLAgentsException($"Number of decisions requested exceeds the set maximum of {DecisionAgentIds.Length}");
            }
#endif
            DecisionAgentIds[index] = entity.Index;
            DecisionAgentEntityIds[index] = entity;
            return new DecisionRequest(index, this);
        }

        /// <summary>
        /// Signals that the Agent has terminated its episode. The task ended properly, either in failure or in success.
        /// The EpisodeTermination struct this method returns can be used to provide data necessary for the Agent to
        /// train properly.
        /// </summary>
        /// <param name="entity"> The Entity whose episode ended. The Entity is used to track
        /// sequences of actions of an Agent.</param>
        /// <returns></returns>
        public EpisodeTermination EndEpisode(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new MLAgentsException($"Invalid operation, cannot end episode on a non-initialized Policy");
            }
#endif
            var index = TerminationCounter.Increment() - 1;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index > TerminationAgentIds.Length)
            {
                throw new MLAgentsException($"Number of termination notifications exceeds the set maximum of {TerminationAgentIds.Length}");
            }
#endif
            TerminationAgentIds[index] = entity.Index;
            TerminationStatus[index] = false;
            return new EpisodeTermination(index, this);
        }

        /// <summary>
        /// Signals that the Agent episode has been interrupted. The task could not end properly.
        /// The EpisodeTermination struct this method returns can be used to provide data necessary for the Agent to
        /// train properly.
        /// </summary>
        /// <param name="entity"> The Entity whose episode ended. The Entity is used to track
        /// sequences of actions of an Agent.</param>
        /// <returns></returns>
        public EpisodeTermination InterruptEpisode(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsCreated)
            {
                throw new MLAgentsException($"Invalid operation, cannot end episode on a non-initialized Policy");
            }
#endif
            var index = TerminationCounter.Increment() - 1;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index > TerminationAgentIds.Length)
            {
                throw new MLAgentsException($"Number of termination notifications exceeds the set maximum of {TerminationAgentIds.Length}");
            }
#endif
            TerminationAgentIds[index] = entity.Index;
            TerminationStatus[index] = true;
            return new EpisodeTermination(index, this);
        }
    }
}
