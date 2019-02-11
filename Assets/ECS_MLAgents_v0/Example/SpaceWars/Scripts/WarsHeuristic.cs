using ECS_MLAgents_v0.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    public class WarsHeuristic : IAgentDecision
    {
        public JobHandle DecideBatch(ref NativeArray<float> sensor,
            ref NativeArray<float> actuator,
            int sensorSize,
            int actuatorSize,
            int nAgents,
            JobHandle handle)
        {
            for (int i = 0; i < nAgents; i++)
            {
                var pos = new float3(
                    sensor[i * sensorSize + 0],
                    sensor[i * sensorSize + 1],
                    sensor[i * sensorSize + 2]
                    );
                var rot = new quaternion(
                    sensor[i * sensorSize + 3],
                    sensor[i * sensorSize + 4],
                    sensor[i * sensorSize + 5],
                    sensor[i * sensorSize + 6]
                    );

                var tmp = Attack(ref pos, ref rot, 1);
//                var tmp = new Steering();
//                if (IsAtLeastThisFar(ref pos, 0.05f).x)
//                {
//                    tmp = Attack(ref pos, ref rot, -1);
//                }
//                else if (IsAtLeastThisFar(ref pos, 0.3f).y)
//                {
//                    tmp = Attack(ref pos, ref rot, 1);
//                }

                actuator[i * actuatorSize + 0] = tmp.YAxis;
                actuator[i * actuatorSize + 1] = tmp.XAxis;
                actuator[i * actuatorSize + 2] = tmp.Shoot;
//                if (IsAtLeastThisFar(ref pos, 0.2f))
//                {
//                    var tmp = Attack(ref pos, ref rot);
//                    actuator[i * actuatorSize + 0] = tmp.YAxis;
//                    actuator[i * actuatorSize + 1] = tmp.XAxis;
//                    actuator[i * actuatorSize + 2] = tmp.Shoot;
//                }
                
            }

            return handle;
        }

        private bool2 IsAtLeastThisFar(ref float3 pos, float distance)
        {
            return new bool2(
                pos.x * pos.x + pos.y * pos.y + pos.z * pos.z < distance * distance,
                pos.x * pos.x + pos.y * pos.y + pos.z * pos.z > distance * distance
                );
        }
        
        private bool MustFire(ref float3 pos, ref quaternion rot)
        {
            // TODO : Implement
            return false;
        }
        
        private Steering Attack(ref float3 position, ref quaternion rot, int attack)
        {
            if (math.abs(position.x) < 0.0001f)
            {
                position = new float3(1,1,1);
            }
            var forwardRelativeRef = math.mul(rot, new float3(0, 0, 1));
            var cross = attack * math.cross(math.normalize(position), forwardRelativeRef);

            var yRot = math.dot(cross, math.mul(rot, new float3(0, 1, 0)));
            var xRot = math.dot(cross, math.mul(rot, new float3(1, 0, 0)));
            
            return new Steering
            {   
//                XAxis = cross.x > 0 ? 1.0f:-1.0f,
//                YAxis = cross.y > 0 ? 1.0f:-1.0f,
//                YAxis = targetRelativeToShip.x > 0 ? 1.0f:-1.0f,
                YAxis = yRot,// > 0 ? 1.0f:-1.0f,
                XAxis = xRot,// > 0 ? 1.0f:-1.0f,
                Shoot = cross.x* cross.x + cross.y*cross.y+cross.z*cross.z < 0.01f ? 1f : 0f//attack
//                XAxis = targetRelativeToShip.y < 0 ? 1.0f:-1.0f,
//                Shoot = targetRelativeToShip.z < 0 ? 1.0f:-1.0f,
//                Shoot = MustFire(ref position, ref rot) ? 1f : 0f,
            };
        }
        
    }
}
