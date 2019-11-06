using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;


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

        // [NativeDisableUnsafePtrRestriction] public JobHandle FinalJobHandle;

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
            AgentIds = new NativeArray<Entity>(capacity, Allocator.Persistent);

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

        public int CollectData<T>(
            Entity entity,
            T sensor,
            float reward = 0f,
            bool done = false
            ) where T : struct
        {
            if (UnsafeUtility.SizeOf<T>() != SensorFloatSize * sizeof(float))
            {
                // Need to hadle safety but it is not possible to store System.Type (class) in a struct
                Debug.Log("Error in the type of sensor");
            }
            // https://docs.unity3d.com/Packages/com.unity.jobs@0.0/manual/custom_job_types.html#custom-job-types
            // This is on how to create a NativeCounter

            int index = AgentCounter.ToConcurrent().Increment() - 1;
            //Sensor
            // Maybe can optimize here
            int start = SensorFloatSize * index;
            var tmp = Sensors.Slice(start, SensorFloatSize).SliceConvert<T>();
            tmp[0] = sensor;
            // // Reward
            Rewards[index] = reward;
            // // Done
            DoneFlags[index] = done;
            // // AgentId
            AgentIds[index] = entity;

            return index;
        }


    }
}
