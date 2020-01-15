using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;


namespace DOTS_MLAgents.Core
{

    public struct MLAgentsWorldWrapper
    {
        // A wrapper to expose only the methods useful to the user
        private MLAgentsWorld world;
        public MLAgentsWorld.DecisionRequest RequestDecision(Entity ent)
        {
            return world.RequestDecision(ent);
        }
        public MLAgentsWorld UnsafeGetMLAgentsWorld()
        {
            return world;
        }
    }

    public enum ActionType : int
    {
        DISCRETE = 0,
        CONTINUOUS = 1,
    }
    public struct MLAgentsWorld : IDisposable
    {
        [ReadOnly] public NativeArray<int3> SensorShapes;
        [ReadOnly] public int ActionSize;
        [ReadOnly] public ActionType ActionType;
        [ReadOnly] public NativeArray<int> DiscreteActionBranches;

        [ReadOnly] public NativeArray<int> ObservationOffsets;

        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> Sensors;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> Rewards;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<bool> DoneFlags;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<bool> MaxStepFlags;
        [NativeDisableParallelForRestriction] public NativeArray<Entity> AgentIds;
        [NativeDisableParallelForRestriction] public NativeArray<bool> ActionMasks;

        //https://forum.unity.com/threads/is-it-okay-to-read-a-nativecounter-concurrents-value-in-a-parallel-job.533037/
        [NativeDisableParallelForRestriction] public NativeCounter AgentCounter;
        [NativeDisableParallelForRestriction] public NativeCounter ActionCounter;

        [NativeDisableParallelForRestriction] public NativeArray<float> ContinuousActuators;
        [NativeDisableParallelForRestriction] public NativeArray<int> DiscreteActuators;
        [NativeDisableParallelForRestriction] public NativeArray<Entity> ActionAgentIds; // Keep track of the Ids for the next action step
        [NativeDisableParallelForRestriction] public NativeArray<bool> ActionDoneFlags; // Keep track of the Done flags for the next action step



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
                if (branch > world.DiscreteActionBranches.Length)
                {
                    throw new MLAgentsException("Unknown action branch used when setting mask.");
                }
                if (actionIndex > world.DiscreteActionBranches[branch])
                {
                    throw new MLAgentsException("Index is out of bounds for requested action mask.");
                }
                var trueMaskIndex = world.DiscreteActionBranches.CumSumAt(branch) + actionIndex;
                world.ActionMasks[trueMaskIndex + world.DiscreteActionBranches.Sum() * index] = true;
                return this;
            }

            public DecisionRequest SetObservation<T>(int sensorNumber, T sensor) where T : struct
            {
                int inputSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new MLAgentsException(
                        "Cannot set observation due to incompatible size of the input. Expected size : " + expectedInputSize + ", received size : " + inputSize);
                }
                int start = world.ObservationOffsets[sensorNumber];
                start += inputSize * index;
                var tmp = world.Sensors.Slice(start, inputSize).SliceConvert<T>();
                tmp[0] = sensor;
                return this;
            }

            public DecisionRequest SetObservationFromSlice(int sensorNumber, [ReadOnly] NativeSlice<float> obs)
            {
                int inputSize = obs.Length;
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new MLAgentsException(
                        "Cannot set observation due to incompatible size of the input. Expected size : " + expectedInputSize + ", received size : " + inputSize);
                }
                int start = world.ObservationOffsets[sensorNumber];
                start += inputSize * index;
                world.Sensors.Slice(start, inputSize).CopyFrom(obs);
                return this;
            }
        }

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

        public void ResetActionsCounter()
        {
            ActionCounter.Count = 0;
        }

        public void ResetDecisionsCounter()
        {
            AgentCounter.Count = 0;
        }

        public void SetActionReady()
        {
            ActionCounter.Count = AgentCounter.Count;
            ActionDoneFlags.CopyFrom(DoneFlags);
            ActionAgentIds.CopyFrom(AgentIds);
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
            if (index > AgentIds.Length)
            {
                throw new MLAgentsException("Number of decisions requested exceeds the set maximum of " + AgentIds.Length);
            }
            AgentIds[index] = entity;
            return new DecisionRequest(index, this);
        }
    }
}
