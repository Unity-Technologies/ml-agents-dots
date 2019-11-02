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
    public class MLAgentsWorldSystem : JobComponentSystem // Should this be a ISimulation from Unity.Physics ?
    {

        public const int n_threads = 2;

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
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) { return inputDeps; }
        public JobHandle ManualUpdate(JobHandle inputDeps)
        {
            foreach (var val in WorldDict)
            {
                var world = val.Value;



                inputDeps.Complete(); // Need to complete here to ensure we have the right Agent Count


                var j = new CopyActuatorData
                {
                    sensorData = world.DataCollector.Sensors, // Just the identity for now
                    actuatorData = world.ActuatorDataHolder.Actuators
                };



                inputDeps = j.Schedule(
                    world.DataCollector.AgentCounter.Count * world.ActuatorDataHolder.ActuatorFloatSize,
                    n_threads,
                    inputDeps);

                var k = new CopyAgentIdData
                {
                    agentData = world.DataCollector.AgentIds,
                    actuatorData = world.ActuatorDataHolder.AgentIds
                };
                inputDeps = k.Schedule(
                    world.DataCollector.AgentCounter.Count,
                    n_threads, inputDeps);


                // string s = "";
                // for (int i = 0; i < DataCollector.AgentCounter; i++)
                // {
                //     s += world.ActuatorDataHolder.AgentIds[i].Index + " ";
                //     // s += world.DataCollector.AgentIds[i].Index + " ";
                // }
                // Debug.Log(s);


                // This is not a job ...s
                // world.ActuatorDataHolder.NumAgents = world.DataCollector.AgentCounter.Count;


                // world.DataCollector.AgentCounter.Count = 0;

                var l = new ResetCounterJob
                {
                    SensorCounter = world.DataCollector.AgentCounter,
                    actionData = world.ActuatorDataHolder
                };
                inputDeps = l.Schedule(inputDeps);
            }

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

    public struct CopyAgentIdData : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> agentData;
        [WriteOnly] public NativeArray<Entity> actuatorData;
        public void Execute(int i)
        {
            actuatorData[i] = agentData[i];
        }
    }

    public struct ResetCounterJob : IJob
    {
        public NativeCounter SensorCounter;
        public ActionDataHolder actionData;

        public void Execute()
        {
            actionData.NumAgents = SensorCounter.Count;
            SensorCounter.Count = 0;

        }
    }


}
