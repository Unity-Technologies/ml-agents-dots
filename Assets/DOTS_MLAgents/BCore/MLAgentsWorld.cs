using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;
using UnityEngine;


namespace DOTS_MLAgents.Core
{

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

        [NativeDisableParallelForRestriction] public NativeArray<float> ContinuousActuators;
        [NativeDisableParallelForRestriction] public NativeArray<int> DiscreteActuators;

        //https://forum.unity.com/threads/is-it-okay-to-read-a-nativecounter-concurrents-value-in-a-parallel-job.533037/
        [NativeDisableParallelForRestriction] public NativeCounter AgentCounter;

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
                // TODO
                return this;
            }

            public DecisionRequest SetObservation<T>(int sensorNumber, T sensor) where T : struct
            {
                int inputSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
                int3 s = world.SensorShapes[sensorNumber];
                int expectedInputSize = s.x * math.max(1, s.y) * math.max(1, s.z);
                if (inputSize != expectedInputSize)
                {
                    throw new Exception("Error");
                    // Need to handle safety but it is not possible to store System.Type (class) in a struct
                    //Debug.Log("Error in the type of sensor"); No strings in burst
                }
                int start = world.ObservationOffsets[sensorNumber];
                start += inputSize * index;
                if (start > 30000)
                {
                    Debug.Log(sensorNumber + "  " + world.ObservationOffsets[sensorNumber] + "  " + index + "  " + inputSize);

                }
                var tmp = world.Sensors.Slice(start, inputSize).SliceConvert<T>();
                tmp[0] = sensor;
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
                    throw new Exception("TODO");
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
                currentOffset += s.x * math.max(1, s.y) * math.max(1, s.z);
            }

            Sensors = new NativeArray<float>(maximumNumberAgents * currentOffset, Allocator.Persistent);
            Rewards = new NativeArray<float>(maximumNumberAgents, Allocator.Persistent);
            DoneFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent);
            MaxStepFlags = new NativeArray<bool>(maximumNumberAgents, Allocator.Persistent);
            AgentIds = new NativeArray<Entity>(maximumNumberAgents, Allocator.Persistent);

            int nMasks = 0;
            if (ActionType == ActionType.DISCRETE)
            {
                foreach (int branchSize in DiscreteActionBranches)
                {
                    nMasks += branchSize;
                }
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

            ContinuousActuators = new NativeArray<float>(maximumNumberAgents * caSize, Allocator.Persistent);
            DiscreteActuators = new NativeArray<int>(maximumNumberAgents * daSize, Allocator.Persistent);

            AgentCounter = new NativeCounter(Allocator.Persistent);
            // FinalJobHandle = new JobHandle();
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
            ContinuousActuators.Dispose();
            DiscreteActuators.Dispose();
            AgentCounter.Dispose();
        }

        public DecisionRequest RequestDecision(Entity entity)
        {
            var index = AgentCounter.ToConcurrent().Increment() - 1;
            if (index > AgentIds.Length)
            {
                throw new Exception("ERROR TODO");
            }
            AgentIds[index] = entity;
            return new DecisionRequest(index, this);
        }
    }
}
