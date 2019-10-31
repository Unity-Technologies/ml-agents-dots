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
    public class MLAgentsWorldSystem : JobComponentSystem
    {

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
            var newWorld = new MLAgentsWorld(typeof(TS), typeof(TA), 5);
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


                inputDeps.Complete();
                world.ActuatorData.AgentIndices.Clear();


                var j = new CopyActuatorData
                {
                    data = world.DataCollector.Sensors, // Just the identity for now
                    actuatorData = world.ActuatorData
                };
                inputDeps = j.Schedule(DataCollector.AgentCounter * world.ActuatorData.ActuatorSize, 2, inputDeps);
                inputDeps.Complete();


                var j2 = new CreateAgentIdMapping
                {
                    currentAgents = world.DataCollector.AgentIds,
                    actuatorData = world.ActuatorData
                };
                inputDeps = j2.Schedule(DataCollector.AgentCounter, /*Broken if I dont */DataCollector.AgentCounter, inputDeps);




                inputDeps.Complete();





                DataCollector.AgentCounter = 0;
            }

            return inputDeps;
        }
    }



    public struct CopyActuatorData : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> data;
        [WriteOnly] public ActuatorData actuatorData;
        public void Execute(int i)
        {
            actuatorData.Actuators[i] = data[i];
        }
    }

    public struct CreateAgentIdMapping : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> currentAgents;
        [WriteOnly] public ActuatorData actuatorData;

        public void Execute(int i)
        {

            var suc = actuatorData.AgentIndices.TryAdd(currentAgents[i], i);
            // Adding to hashmap concurrently is harder than this.
            if (!suc)
            {
                Debug.LogError("Fail : " + currentAgents[i] + " " + i + " " + actuatorData.AgentIndices.TryGetValue(currentAgents[i], out _));
            }
        }
    }

}
