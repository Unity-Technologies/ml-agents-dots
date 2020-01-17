using System;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS_MLAgents.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// This component will represent the acceleration of the spheres
    /// </summary>
    [Serializable]
    public struct Acceleration : IComponentData
    {
        public float3 Value;
    }
}
