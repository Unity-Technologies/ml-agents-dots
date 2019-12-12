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
using System.IO;

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

        public const Mode MODE = Mode.COMMUNICATION;

        public const int n_threads = 64;

        public const int max_agents = 10000;

        private JobHandle dependencies;
        public JobHandle FinalJobHandle;

        private SharedMemoryCom com;

        private Dictionary<string, MLAgentsWorld> WorldDict;

        private Dictionary<string, BarracudaWorldProcessor> ModelStore;
        public MLAgentsWorld GetExistingWorld(string policyId)
        {
            if (WorldDict.ContainsKey(policyId))
            {
                return WorldDict[policyId];
            }
            else
            {
                throw new System.Exception("TODO");
            }
        }
        public void SubscribeWorld(string policyId, MLAgentsWorld world)
        {
            if (!WorldDict.ContainsKey(policyId))
            {
                WorldDict[policyId] = world;
            }
            else
            {
                throw new System.Exception("TODO");
            }
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
                com = new SharedMemoryCom(Path.Combine(Path.GetTempPath(), "ml-agents", "default"));
                com.Advance();
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


            Debug.Log("MODE : " + MODE);

            if (MODE == Mode.COMMUNICATION)
            {
                foreach (var val in WorldDict)
                {
                    var world = val.Value;
                    com.WriteWorld(val.Key, world);
                }
                // com.WriteSideChannelData(new byte[4]);
                com.SetUnityReady();
                var command = com.Advance(); // Should be called only once, not per world as right now

                Debug.Log(command);
                // Debug.Log(com.ReadAndClearSideChannelData()?.Length);

                foreach (var val in WorldDict)
                {
                    var world = val.Value;
                    com.LoadWorld(val.Key, world);
                }
            }
            else if (MODE == Mode.HEURISTIC)
            {
                foreach (var val in WorldDict)
                {
                    var world = val.Value;
                    var j = new CopyActuatorData
                    {
                        sensorData = world.Sensors, // Just the identity for now
                        actuatorData = world.ContinuousActuators
                    };
                    FinalJobHandle = j.Schedule(
                                        world.AgentCounter.Count * world.ActionSize,
                                        n_threads,
                                        FinalJobHandle);
                }
            }
            else if (MODE == Mode.BARRACUDA)
            {
                // ModelStore[val.Key].ProcessWorld(world);
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
            foreach (var kv in WorldDict)
            {
                kv.Value.Dispose();
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
