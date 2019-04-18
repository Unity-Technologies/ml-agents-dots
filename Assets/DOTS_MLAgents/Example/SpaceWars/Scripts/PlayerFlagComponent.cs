using System;
using Unity.Entities;


namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct PlayerFlag : IComponentData
    {
        public int Flag;
    }
    
    public class PlayerFlagComponent : ComponentDataProxy<PlayerFlag> {}
}
