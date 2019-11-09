using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using System;
using UnityEngine;


namespace DOTS_MLAgents.Core
{
    public struct MLAgentsWorld : IDisposable
    {
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> Sensors;
        [NativeDisableParallelForRestriction] [WriteOnly] private NativeArray<float> Rewards;
        [NativeDisableParallelForRestriction] [WriteOnly] private NativeArray<bool> DoneFlags;
        [NativeDisableParallelForRestriction] public NativeArray<Entity> AgentIds;
        [ReadOnly] public int SensorFloatSize;
        [ReadOnly] public int ActuatorFloatSize;
        [NativeDisableParallelForRestriction] public NativeArray<float> Actuators;

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

            public DecisionRequest SetObservation<T>(T sensor) where T : struct
            {
                if (UnsafeUtility.SizeOf<T>() != world.SensorFloatSize * sizeof(float))
                {
                    // Need to handle safety but it is not possible to store System.Type (class) in a struct
                    //Debug.Log("Error in the type of sensor"); No strings in burst
                }
                int start = world.SensorFloatSize * index;
                var tmp = world.Sensors.Slice(start, world.SensorFloatSize).SliceConvert<T>();
                tmp[0] = sensor;
                return this;
            }
        }

        public MLAgentsWorld(Type sensorType, Type actuatorType, int capacity = 100)
        {
            SensorFloatSize = UnsafeUtility.SizeOf(sensorType) / sizeof(float);
            Sensors = new NativeArray<float>(capacity * SensorFloatSize, Allocator.Persistent);
            Rewards = new NativeArray<float>(capacity, Allocator.Persistent);
            DoneFlags = new NativeArray<bool>(capacity, Allocator.Persistent);
            AgentIds = new NativeArray<Entity>(capacity, Allocator.Persistent);
            AgentCounter = new NativeCounter(Allocator.Persistent);

            ActuatorFloatSize = UnsafeUtility.SizeOf(actuatorType) / sizeof(float);
            Actuators = new NativeArray<float>(capacity * ActuatorFloatSize, Allocator.Persistent);
            // FinalJobHandle = new JobHandle();
        }
        public void Dispose()
        {
            Sensors.Dispose();
            Rewards.Dispose();
            DoneFlags.Dispose();
            AgentIds.Dispose();
            Actuators.Dispose();
            AgentCounter.Dispose();
        }

        public DecisionRequest RequestDecision(Entity entity)
        {
            var index = AgentCounter.ToConcurrent().Increment() - 1;
            AgentIds[index] = entity;
            return new DecisionRequest(index, this);
        }
    }
}
