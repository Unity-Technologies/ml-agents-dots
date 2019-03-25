using ECS_MLAgents_v0.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    public class WarsHeuristic : IAgentDecision<ShipSensor, Steering>
    {
        public void BatchProcess(ref NativeArray<ShipSensor> sensors, ref NativeArray<Steering> actuators, int offset = 0, int size = -1)
        {
            for (int i = 0; i < sensors.Length; i++)
            {
                var pos = sensors[i].Position;
                var rot = sensors[i].Rotation;

                var tmp = Attack(ref pos, ref rot, 1);

                actuators[i] = new Steering{
                    XAxis = tmp.XAxis,
                    YAxis = tmp.YAxis,
                    Shoot = tmp.Shoot
                };
                
            }
        }

        private Steering Attack(ref float3 position, ref quaternion rot, int attack)
        {
            var forwardRelativeRef = math.mul(rot, new float3(0, 0, 1));
            var cross = attack * math.cross(position, forwardRelativeRef);

            var yRot = math.dot(cross, math.mul(rot, new float3(0, 1, 0)));
            var xRot = math.dot(cross, math.mul(rot, new float3(1, 0, 0)));
            
            return new Steering
            {   

                YAxis = yRot,
                XAxis = xRot,
                Shoot = cross.x* cross.x + cross.y*cross.y+cross.z*cross.z < 0.01f ? 1f : 0f//attack
            };
        }
        
    }
}
