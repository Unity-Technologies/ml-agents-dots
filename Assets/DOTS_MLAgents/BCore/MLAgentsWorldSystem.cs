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
using Barracuda;

namespace DOTS_MLAgents.Core
{

    public enum Mode
    {
        COMMUNICATION,
        BARRACUDA,
        HEURISTIC
    }
    // [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MLAgentsWorldSystem : JobComponentSystem // Should this be a ISimulation from Unity.Physics ?
    {

        public const Mode MODE = Mode.BARRACUDA;

        public const int n_threads = 64;

        public const int max_agents = 10000;

        private JobHandle dependencies;
        public JobHandle FinalJobHandle;

        private SharedMemoryCom com;

        private Dictionary<string, MLAgentsWorld> WorldDict;

        private Dictionary<string, BarracudaWorldProcessor> ModelStore;
        public MLAgentsWorld GetExistingMLAgentsWorld<TS, TA>(string policyId)
        where TS : struct
        where TA : struct
        {
            // Can this be passed by reference ?
            if (WorldDict.ContainsKey(policyId))
            {
                return WorldDict[policyId];
            }
            Debug.Log("A whole new world : " + policyId);
            var newWorld = new MLAgentsWorld(typeof(TS), typeof(TA), max_agents);
            WorldDict[policyId] = newWorld;
            return newWorld;
        }

        public void SetModel(string policyId, NNModel model)
        {
            ModelStore.Add(policyId, new BarracudaWorldProcessor(model));
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

        protected override void OnCreate()
        {
            WorldDict = new Dictionary<string, MLAgentsWorld>();
            ModelStore = new Dictionary<string, BarracudaWorldProcessor>();
            dependencies = new JobHandle();
            FinalJobHandle = new JobHandle();
            if (MODE == Mode.COMMUNICATION)
            {
                com = new SharedMemoryCom("shared_communication_file.txt");
            }
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

                Debug.Log("MODE : " + MODE + "  " + val.Key);

                if (MODE == Mode.COMMUNICATION)
                {
                    com.WriteWorld(world);
                    com.Advance();
                    com.LoadWorld(world);
                }
                else if (MODE == Mode.HEURISTIC)
                {
                    var j = new CopyActuatorData
                    {
                        sensorData = world.Sensors, // Just the identity for now
                        actuatorData = world.Actuators
                    };
                    FinalJobHandle = j.Schedule(
                                        world.AgentCounter.Count * world.ActuatorFloatSize,
                                        n_threads,
                                        FinalJobHandle);
                }
                else if (MODE == Mode.BARRACUDA)
                {
                    ModelStore[val.Key].ProcessWorld(world);
                }



            }

            inputDeps = JobHandle.CombineDependencies(inputDeps, FinalJobHandle);
            inputDeps.Complete();
            return inputDeps;
        }

        protected override void OnDestroy()
        {
            if (MODE == Mode.COMMUNICATION)
            {
                com.Dispose();
            }
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





}
