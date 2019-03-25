using ECS_MLAgents_v0.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

namespace ECS_MLAgents_v0.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// This decision heuristic assigns some value to the actuators based on the sensor. The
    /// resulting motion for sphere assigned to this decision is a fast oscillation around the
    /// center point.
    /// </summary>
    public class HeuristicSpace : IAgentDecision<Position, Acceleration>
    {
        private float3 _center;
        private float _strength;

        /// <summary>
        /// Returns a new Heuristic Space IAgentDecision
        /// </summary>
        /// <param name="center">The center point  of the oscillation</param>
        /// <param name="strength">The strength of the oscillation</param>
        public HeuristicSpace(float3 center, float strength)
        {
            _center = center;
            _strength = strength;
        }
        
        public void BatchProcess(ref NativeArray<Position> sensors, ref NativeArray<Acceleration> actuators, int offset = 0, int size = -1)
        {

            var nAgents = sensors.Length;
            if (size ==-1){
                size = sensors.Length - offset;
            }
            float3 pos = new float3();
            for (int n = offset; n < size + offset; n++)
            {
                pos = sensors[n].Value;
                
                var d = (pos.x * pos.x+ pos.y * pos.y + pos.z * pos.z);

                pos = -_strength * pos;

                actuators[n] = new Acceleration{
                    Value = new float3(-100f*pos.z/d +pos.x, 0 + pos.y, 50f*pos.x/d + pos.z)
                };
            }
        }
    }
}
