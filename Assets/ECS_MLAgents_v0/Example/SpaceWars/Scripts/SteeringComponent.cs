using System;
using Unity.Entities;
using UnityEngine.Serialization;


namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Steering : IComponentData
    {
        public float YAxis;
        public float XAxis;
        public float Shoot;
    }
    
    public class SteeringComponent : ComponentDataWrapper<Steering> {}
}
