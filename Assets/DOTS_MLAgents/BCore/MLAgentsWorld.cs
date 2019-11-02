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

    public unsafe struct DataCollector
    {
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> Sensors;
        [NativeDisableParallelForRestriction] [WriteOnly] private NativeArray<float> Rewards;
        [NativeDisableParallelForRestriction] [WriteOnly] private NativeArray<bool> DoneFlags;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<Entity> AgentIds;
        [ReadOnly] public int SensorFloatSize;

        //https://forum.unity.com/threads/is-it-okay-to-read-a-nativecounter-concurrents-value-in-a-parallel-job.533037/
        [NativeDisableParallelForRestriction] public NativeCounter AgentCounter;

        public unsafe DataCollector(Type type, int capacity = 100)
        {
            SensorFloatSize = UnsafeUtility.SizeOf(type) / sizeof(float);
            Sensors = new NativeArray<float>(capacity * SensorFloatSize, Allocator.Persistent);
            Rewards = new NativeArray<float>(capacity, Allocator.Persistent);
            DoneFlags = new NativeArray<bool>(capacity, Allocator.Persistent);
            AgentIds = new NativeArray<Entity>(capacity, Allocator.Persistent);
            AgentCounter = new NativeCounter(Allocator.Persistent);
        }

        // Does it make sense to identify by Entity?

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

        public void Dispose()
        {
            Sensors.Dispose();
            Rewards.Dispose();
            DoneFlags.Dispose();
            AgentIds.Dispose();
            AgentCounter.Dispose();
        }
    }


    public struct ActionDataHolder : IDisposable
    {
        // Huge issue around the indexing but EntityManager solved it...
        [NativeDisableParallelForRestriction] public NativeArray<float> Actuators;

        [NativeDisableParallelForRestriction] public NativeArray<Entity> AgentIds;
        [ReadOnly] public int ActuatorFloatSize;

        public int NumAgents;

        public ActionDataHolder(Type type, int capacity = 100)
        {
            ActuatorFloatSize = UnsafeUtility.SizeOf(type) / sizeof(float);
            Actuators = new NativeArray<float>(capacity * ActuatorFloatSize, Allocator.Persistent);
            AgentIds = new NativeArray<Entity>(capacity, Allocator.Persistent);
            NumAgents = 0;
        }

        public void Dispose()
        {
            Actuators.Dispose();
            AgentIds.Dispose();
        }

    }

    public struct MLAgentsWorld : IDisposable
    {
        public DataCollector DataCollector;
        public ActionDataHolder ActuatorDataHolder;

        public MLAgentsWorld(Type sensorType, Type actuatorType, int capacity = 100)
        {
            DataCollector = new DataCollector(sensorType, capacity);
            ActuatorDataHolder = new ActionDataHolder(actuatorType, capacity);
        }
        public void Dispose()
        {
            DataCollector.Dispose();
            ActuatorDataHolder.Dispose();
        }
    }
}
