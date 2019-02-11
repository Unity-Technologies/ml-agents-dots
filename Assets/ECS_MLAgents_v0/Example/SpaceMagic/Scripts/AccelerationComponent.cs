using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// This component will represent the acceleration of the spheres
    /// </summary>
    [Serializable]
    public struct Acceleration : IComponentData
    {
        public float3 Value;
    }
    
    /// <summary>
    /// This wrapper only allows us to add this IComponentData as a Component to the sphere prefab
    /// </summary>
    public class AccelerationComponent : ComponentDataWrapper<Acceleration> { }
}
