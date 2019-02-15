using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Ship : IComponentData
    {
        public float ReloadTime;
        public int Fire;
        public float3 TargetOffset;
        public float MaxReloadTime;
    }
    
    public class ShipComponent : ComponentDataWrapper<Ship> {}
}
