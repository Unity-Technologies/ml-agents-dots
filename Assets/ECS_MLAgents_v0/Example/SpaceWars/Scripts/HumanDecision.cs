using ECS_MLAgents_v0.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    public class HumanDecision : IAgentDecision
    {
        
        public JobHandle DecideBatch(ref NativeArray<float> sensor,
            ref NativeArray<float> actuator,
            int sensorSize,
            int actuatorSize, 
            int nAgents,
            JobHandle handle)
        {
            var input = new float3();
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                input.x = -1;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                input.x = 1;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                input.y = -1;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                input.y = 1;
            }
                
            if (Input.GetKey(KeyCode.Space))
            {
                input.z = 1;
            }
            
            for (int n = 0; n < nAgents; n++)
            {
                actuator[n * 3 + 0] = input.x;
                actuator[n * 3 + 1] = input.y;
                actuator[n * 3 + 2] = input.z;
            }
            return handle;
        }
    }
}
