using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Example.ZeroK.Scripts
{
    /// <summary>
    /// This IComponentData hold the velocity of each sphere.
    /// </summary>
    [Serializable]
    public struct Speed : IComponentData
    {
        public float3 Value;
    }

    public struct Sensor : IComponentData
    {
        public float Reward;
        public float Done;
        public float Timer;
        public float3 Position;
    }
    
    
}