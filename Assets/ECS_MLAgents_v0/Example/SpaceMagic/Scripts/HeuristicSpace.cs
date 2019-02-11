using ECS_MLAgents_v0.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// This decision heuristic assigns some value to the actuators based on the sensor. The
    /// resulting motion for sphere assigned to this decision is a fast oscillation around the
    /// center point.
    /// </summary>
    public class HeuristicSpace : IAgentDecision
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
        
        public JobHandle DecideBatch(ref NativeArray<float> sensor,
            ref NativeArray<float> actuator,
            int sensorSize,
            int actuatorSize, 
            int nAgents,
            JobHandle handle)
        {
            var pos = new float3();
            for (int n = 0; n < nAgents; n++)
            {
                pos.x =sensor[n * 3 + 0] - _center.x;
                pos.y =sensor[n * 3 + 1] - _center.y;
                pos.z =sensor[n * 3 + 2] - _center.z;
                var d = (pos.x * pos.x + pos.y * pos.y + pos.z * pos.z);

                pos = -_strength * pos;

                actuator[n * 3 + 0] = -100f*pos.z/d +pos.x;
                actuator[n * 3 + 1] = 0 + pos.y;
                actuator[n * 3 + 2] = 50f*pos.x/d + pos.z;
            }
            return handle;
        }
    }
}
