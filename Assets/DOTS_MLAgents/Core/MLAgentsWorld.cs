using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;

namespace DOTS_MLAgents.Core
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

        [NativeDisableParallelForRestriction] [WriteOnly] internal NativeArray<float> Sensors;
        [NativeDisableParallelForRestriction] [WriteOnly] internal NativeArray<float> Rewards;
        [NativeDisableParallelForRestriction] [WriteOnly] internal NativeArray<bool> DoneFlags;
        [NativeDisableParallelForRestriction] [WriteOnly] internal NativeArray<bool> MaxStepFlags;
        [NativeDisableParallelForRestriction] internal NativeArray<Entity> AgentIds;
        [NativeDisableParallelForRestriction] internal NativeArray<bool> ActionMasks;

        //https://forum.unity.com/threads/is-it-okay-to-read-a-nativecounter-concurrents-value-in-a-parallel-job.533037/
        [NativeDisableParallelForRestriction] internal NativeCounter AgentCounter;
        [NativeDisableParallelForRestriction] internal NativeCounter ActionCounter;

        [NativeDisableParallelForRestriction] internal NativeArray<float> ContinuousActuators;
        [NativeDisableParallelForRestriction] internal NativeArray<int> DiscreteActuators;
        [NativeDisableParallelForRestriction] internal NativeArray<Entity> ActionAgentIds; // Keep track of the Ids for the next action step
        [NativeDisableParallelForRestriction] internal NativeArray<bool> ActionDoneFlags; // Keep track of the Done flags for the next action step



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
                currentOffset += s.x * math.max(1, s.y) * math.max(1, s.z) * maximumNumberAgents;
            }

            Sensors = new NativeArray<float>(currentOffset, Allocator.Persistent);
            Rewards = new NativeArray<float>(maximumNumberAgents, Allocator.Persistent);
            DoneFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent);
            MaxStepFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent);
            AgentIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent);

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

            ContinuousActuators = new NativeArray<float>(maximumNumberAgents * caSize, Allocator.Persistent);
            DiscreteActuators = new NativeArray<int>(maximumNumberAgents * daSize, Allocator.Persistent);
            ActionDoneFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent);
            ActionAgentIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent);
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
            ActionAgentIds.Slice(0, count).CopyFrom(AgentIds.Slice(0, count));
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
            AgentIds.Dispose();
            ActionMasks.Dispose();
            AgentCounter.Dispose();
            ActionCounter.Dispose();
            ContinuousActuators.Dispose();
            DiscreteActuators.Dispose();
            ActionDoneFlags.Dispose();
            ActionAgentIds.Dispose();
        }

        public DecisionRequest RequestDecision(Entity entity)
        {
            var index = AgentCounter.ToConcurrent().Increment() - 1;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index > AgentIds.Length)
            {
                throw new MLAgentsException("Number of decisions requested exceeds the set maximum of " + AgentIds.Length);
            }
#endif
            AgentIds[index] = entity;
            return new DecisionRequest(index, this);
        }


        public struct DecisionRequest
        {
            private int index;
            private MLAgentsWorld world;

            public DecisionRequest(int index, MLAgentsWorld world)
            {
                this.index = index;
                this.world = world;
            }

            public DecisionRequest SetReward(float r)
            {
                world.Rewards[index] = r;
                return this;
            }

            public DecisionRequest HasTerminated(bool done, bool maxStepReached)
            {
                world.DoneFlags[index] = done;
                world.MaxStepFlags[index] = maxStepReached;
                return this;
            }

            public DecisionRequest SetDiscreteActionMask(int branch, int actionIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (branch > world.DiscreteActionBranches.Length)
                {
                    throw new MLAgentsException("Unknown action branch used when setting mask.");
                }
                if (actionIndex > world.DiscreteActionBranches[branch])
                {
                    throw new MLAgentsException("Index is out of bounds for requested action mask.");
                }
#endif
                var trueMaskIndex = world.DiscreteActionBranches.CumSumAt(branch) + actionIndex;
                world.ActionMasks[trueMaskIndex + world.DiscreteActionBranches.Sum() * index] = true;
                return this;
            }

            public DecisionRequest SetObservation<T>(int sensorNumber, T sensor) where T : struct
            {
                int inputSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new MLAgentsException(
                        "Cannot set observation due to incompatible size of the input. Expected size : " + expectedInputSize + ", received size : " + inputSize);
                }
#endif
                int start = world.ObservationOffsets[sensorNumber];
                start += inputSize * index;
                var tmp = world.Sensors.Slice(start, inputSize).SliceConvert<T>();
                tmp[0] = sensor;
                return this;
            }

            public DecisionRequest SetObservationFromSlice(int sensorNumber, [ReadOnly] NativeSlice<float> obs)
            {
                int inputSize = obs.Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new MLAgentsException(
                        "Cannot set observation due to incompatible size of the input. Expected size : " + expectedInputSize + ", received size : " + inputSize);
                }
#endif
                int start = world.ObservationOffsets[sensorNumber];
                start += inputSize * index;
                world.Sensors.Slice(start, inputSize).CopyFrom(obs);
                return this;
            }
        }

    }
}
