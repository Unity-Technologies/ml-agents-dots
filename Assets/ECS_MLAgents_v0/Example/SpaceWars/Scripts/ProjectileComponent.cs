using System;
using Unity.Entities;


namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Projectile : IComponentData
    {
        public float Speed;
    }
    
    public class ProjectileComponent : ComponentDataWrapper<Projectile> {}
}
