using System;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Ship : IComponentData
    {
        public float ReloadTime;
        public int Fire;
        public float3 TargetOffset;
        public float MaxReloadTime;
    }
    
    public class ShipComponent : ComponentDataProxy<Ship> {}
}
