using System;
using Unity.Entities;
using UnityEngine.Serialization;


namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Steering : IComponentData
    {
        public float YAxis;
        public float XAxis;
        public float Shoot;
    }
    
    public class SteeringComponent : ComponentDataProxy<Steering> {}
}
