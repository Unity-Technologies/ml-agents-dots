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
            var newWorld = new MLAgentsWorld(typeof(TS), typeof(TA), 100);
            WorldDict[policyId] = newWorld;
            return newWorld;
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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            foreach (var val in WorldDict)
            {


            }

            return inputDeps;
        }
    }


}
