using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DOTS_MLAgents.Core
{

    public struct DataCollector
    {
        [WriteOnly] public NativeArray<float> Sensors;
        [WriteOnly] private NativeArray<float> Rewards;
        [WriteOnly] private NativeArray<bool> DoneFlags;
        [WriteOnly] public NativeArray<int> AgentIds;
        [ReadOnly] Type SensorType;
        [ReadOnly] int SensorSize;

        public unsafe DataCollector(Type type, int capacity = 100)
        {
            Sensors = new NativeArray<float>(capacity, Allocator.Persistent);
            Rewards = new NativeArray<float>(capacity, Allocator.Persistent);
            DoneFlags = new NativeArray<bool>(capacity, Allocator.Persistent);
            AgentIds = new NativeArray<int>(capacity, Allocator.Persistent);
            SensorType = type;
            SensorSize = UnsafeUtility.SizeOf(type);
        }

        // Does it make sense to identify by Entity?

        public void CollectData<T>(
            int index,
            int id,
            T sensor,
            float reward = 0f,
            bool done = false
            ) where T : struct
        {
            if (typeof(T) != SensorType)
            {
                Debug.Log("Error in the type of sensor");
            }
            //Sensor
            // Maybe can optimize here
            var tmp = Sensors.Slice().SliceConvert<T>();
            tmp[index] = sensor;
            // Reward
            Rewards[index] = reward;
            // Done
            DoneFlags[index] = done;
            // AgentId
            AgentIds[index] = id;
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
        [ReadOnly] public NativeArray<float> Actuators;

        [ReadOnly] Type ActuatorType;
        [ReadOnly] int ActuatorSize;

        public ActuatorData(Type type, int capacity = 100)
        {
            Actuators = new NativeArray<float>(capacity, Allocator.Persistent);
            ActuatorType = type;
            ActuatorSize = UnsafeUtility.SizeOf(type);
        }
        public void GetActuator<T>(int index, out T actuator) where T : struct
        {
            actuator = Actuators.Slice().SliceConvert<T>()[index];
        }

        public void Dispose()
        {
            Actuators.Dispose();
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
