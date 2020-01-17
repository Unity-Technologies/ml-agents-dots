using System;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS_MLAgents.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// This IComponentData hold the velocity of each sphere.
    /// </summary>
    [Serializable]
    public struct Speed : IComponentData
    {
        public float3 Value;
    }

}
