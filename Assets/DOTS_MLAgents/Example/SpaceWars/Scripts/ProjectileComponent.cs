using System;
using Unity.Entities;


namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Projectile : IComponentData
    {
        public float Speed;
    }
    
    public class ProjectileComponent : ComponentDataProxy<Projectile> {}
}
