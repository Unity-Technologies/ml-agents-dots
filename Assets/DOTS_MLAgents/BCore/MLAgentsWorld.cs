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

    public struct DataCollector
    {

        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<float> Sensors;
        [NativeDisableParallelForRestriction] [WriteOnly] private NativeArray<float> Rewards;
        [NativeDisableParallelForRestriction] [WriteOnly] private NativeArray<bool> DoneFlags;
        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<Entity> AgentIds;
        [ReadOnly] private int SensorFloatSize;
        public static int AgentCounter; // must be static so there is a reference, can only have one DataCollector adding data at a time

        public unsafe DataCollector(Type type, int capacity = 100)
        {
            Sensors = new NativeArray<float>(capacity * UnsafeUtility.SizeOf(type), Allocator.Persistent);
            Rewards = new NativeArray<float>(capacity, Allocator.Persistent);
            DoneFlags = new NativeArray<bool>(capacity, Allocator.Persistent);
            AgentIds = new NativeArray<Entity>(capacity, Allocator.Persistent);
            SensorFloatSize = UnsafeUtility.SizeOf(type) / sizeof(float);
            AgentCounter = 0;
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
                Debug.Log("Error in the type of sensor");
            }

            int index = Interlocked.Increment(ref AgentCounter) - 1;
            //Sensor
            // Maybe can optimize here
            int start = SensorFloatSize * index;
            int end = SensorFloatSize * (index + 1);
            var tmp = Sensors.Slice(start, end).SliceConvert<T>();
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
        }
    }


    public struct ActuatorData : IDisposable
    {
        // Huge issue around the indexing but EntityManager solved it...
        [NativeDisableParallelForRestriction] public NativeArray<float> Actuators;

        [NativeDisableParallelForRestriction] public NativeHashMap<Entity, int> AgentIndices;
        [ReadOnly] public int ActuatorSize;

        public ActuatorData(Type type, int capacity = 100)
        {
            Actuators = new NativeArray<float>(capacity * UnsafeUtility.SizeOf(type), Allocator.Persistent);
            ActuatorSize = UnsafeUtility.SizeOf(type) / sizeof(float);
            AgentIndices = new NativeHashMap<Entity, int>(capacity, Allocator.Persistent);
        }
        public void GetActuator<T>(Entity entity, out T actuator) where T : struct
        {
            int index = -1;
            AgentIndices.TryGetValue(entity, out index);
            if (index == -1)
            {
                Debug.Log("The entity did not have an actuator last step");
                // This is why a trigger event job is more appropriate
            }
            int start = ActuatorSize * index;
            int end = ActuatorSize * (index + 1);
            actuator = Actuators.Slice(start, end).SliceConvert<T>()[0];
        }

        public void Dispose()
        {
            Actuators.Dispose();
            AgentIndices.Dispose();
        }

    }

    public struct MLAgentsWorld : IDisposable
    {
        public DataCollector DataCollector;
        public ActuatorData ActuatorData;

        public MLAgentsWorld(Type sensorType, Type actuatorType, int capacity = 100)
        {
            DataCollector = new DataCollector(sensorType, capacity);
            ActuatorData = new ActuatorData(actuatorType, capacity);
        }
        public void Dispose()
        {
            DataCollector.Dispose();
            ActuatorData.Dispose();
        }
    }
}
