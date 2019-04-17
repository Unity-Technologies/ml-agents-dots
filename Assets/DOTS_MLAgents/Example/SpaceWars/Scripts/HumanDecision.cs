using DOTS_MLAgents.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    public class HumanDecision<TS> : IAgentDecision<TS, Steering> 
        where TS : struct, IComponentData
    {
        
        public void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<Steering> actuators, int offset = 0, int size = -1)
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
            if (size ==-1){
                size = sensors.Length - offset;
            }
            for (int n = 0; n < actuators.Length; n++)
            {
                actuators[n] = new Steering{
                    YAxis = input.x,
                    XAxis = input.y,
                    Shoot = input.z
                };
            }
        }
    }
}
