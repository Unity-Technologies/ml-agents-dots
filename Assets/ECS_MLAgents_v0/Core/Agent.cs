using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Core
{
    /*
     * This is the Agent Component, it contains information specific to the Agent such as the
     * reward signal and the done flag.
     */
    [Serializable]
    public struct Agent : IComponentData
    {
        // TODO : Add the Agent IComponentData to the appropriate Entities before the first
        // decision pass
        public float3 Reward;
//        public bool1 Done; // TODO : bool is not blittable 
    }
}
