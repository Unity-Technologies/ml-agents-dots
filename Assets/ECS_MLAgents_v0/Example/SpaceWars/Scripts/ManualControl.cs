//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;
//
//
//namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
//{
//    public class ManualControl : JobComponentSystem
//    {
//        
//        
//        private struct ControlJob : IJobProcessComponentData<Steering>
//        {
//            public float3 input;
//
//            public void Execute(ref Steering steering)
//            {
//                steering.YAxis = input.x;
//                steering.ZAxis = input.y;
//                steering.Shoot = input.z;
//            }
//        }
//
//        protected override JobHandle OnUpdate(JobHandle inputDeps)
//        {
//            float3 input = new float3();
//            if (Input.GetKey(KeyCode.Q))
//            {
//                input.x = -1;
//            }
//            if (Input.GetKey(KeyCode.W))
//            {
//                input.x = 1;
//            }
//            if (Input.GetKey(KeyCode.O))
//            {
//                input.y = -1;
//            }
//            if (Input.GetKey(KeyCode.P))
//            {
//                input.y = 1;
//            }
//            
//            if (Input.GetKeyDown(KeyCode.Space))
//            {
//                input.z = 1;
//            }
//
////            if (math.abs(input.x) < 0.01f && math.abs(input.y) < 0.01f )
////            {
////                return inputDeps;
////            }
//            var job = new ControlJob
//            {
//                input = input
//            };
//            var handle = job.Schedule(this, inputDeps);
//            return handle;
//        }
//    }
//}
