using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using Unity.Entities;


namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct Explosion : IComponentData
    {
        public float TimeSinceBirth;
        public float DeathTime;
        public float GrowthRate;
    }
    
    public class ExplosionComponent : ComponentDataProxy<Explosion> {}
}
