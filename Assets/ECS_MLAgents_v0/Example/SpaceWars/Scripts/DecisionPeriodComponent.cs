using System;
using Unity.Entities;

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct DecisionPeriod : ISharedComponentData
    {
        public int Phase;
    }

}
