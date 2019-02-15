using System;
using Unity.Entities;


namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct PlayerFlag : IComponentData
    {
        public int Flag;
    }
    
    public class PlayerFlagComponent : ComponentDataWrapper<PlayerFlag> {}
}
