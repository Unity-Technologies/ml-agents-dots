using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using DOTS_MLAgents.Core.Inference;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

namespace DOTS_MLAgents.Core
{

    // [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MLAgentsWorldSystem : JobComponentSystem // Should this be a ISimulation from Unity.Physics ?
    {

        public const int n_threads = 4;

        private JobHandle dependencies;
        public JobHandle FinalJobHandle;

        private SharedMemoryCom com;

        private Dictionary<string, MLAgentsWorld> WorldDict;
        public MLAgentsWorld GetExistingMLAgentsWorld<TS, TA>(string policyId)
        where TS : struct
        where TA : struct
        {
            // Can this be passed by reference ?
            if (WorldDict.ContainsKey(policyId))
            {
                return WorldDict[policyId];
            }
            var newWorld = new MLAgentsWorld(typeof(TS), typeof(TA), TestMonoB.N_Agents);
            WorldDict[policyId] = newWorld;
            return newWorld;
        }

        NNModel m_model;
        public void GiveModel(NNModel model)
        {
            m_model = model;
        }
        // constructor with camera or raw data collector ?
        public float GetMLAgentsProperty(string propertyName)
        {
            // Or use delegates ?
            // TODO
            return 0f;
        }
        public void SetNNModel(string policyId, NNModel model)
        {

        }

        protected override void OnCreate()
        {
            WorldDict = new Dictionary<string, MLAgentsWorld>();
            dependencies = new JobHandle();
            FinalJobHandle = new JobHandle();
            com = new SharedMemoryCom("Assets/shared_communication_file.txt");
        }

        public void RegisterDependency(JobHandle handle)
        {
            dependencies = JobHandle.CombineDependencies(handle, dependencies);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        // { return inputDeps; }
        // public JobHandle ManualUpdate(JobHandle inputDeps)
        {
            // Need to complete here to ensure we have the right Agent Count
            dependencies.Complete();
            foreach (var val in WorldDict)
            {
                var world = val.Value;


                /*var j = new CopyActuatorData
                {
                    sensorData = world.Sensors, // Just the identity for now
                    actuatorData = world.Actuators
                };
                FinalJobHandle = j.Schedule(
                                    world.AgentCounter.Count * world.ActuatorFloatSize,
                                    n_threads,
                                    FinalJobHandle);
                                    */

                com.WriteWorld(world);
                com.Advance();
                com.LoadWorld(world);


                var l = new ResetCounterJob
                {
                    SensorCounter = world.AgentCounter,
                };


                FinalJobHandle = l.Schedule(FinalJobHandle);
            }

            inputDeps = JobHandle.CombineDependencies(inputDeps, FinalJobHandle);
            inputDeps.Complete();
            return inputDeps;
        }
    }



    public struct CopyActuatorData : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> sensorData;
        [WriteOnly] public NativeArray<float> actuatorData;
        public void Execute(int i)
        {
            actuatorData[i] = sensorData[i];
        }
    }



    public struct ResetCounterJob : IJob
    {
        public NativeCounter SensorCounter;

        public void Execute()
        {
            SensorCounter.Count = 0;

        }
    }


}
