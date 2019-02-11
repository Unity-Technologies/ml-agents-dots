//using System;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using Unity.Jobs;
//using UnityEngine;
//
//namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
//{
//    public class TargetShipMotion : JobComponentSystem
//    {
//       
//        private struct MoveJob : IJobProcessComponentData<Position, Rotation, TargetShip>
//        {
//
//            public float deltaTime;
//            public float rot_speed;
//            public float fwd_speed;
//            public void Execute(ref Position pos, ref Rotation rot, ref TargetShip ship)
//            {
//                rot.Value = math.mul(
//                    rot.Value,
//                    quaternion.AxisAngle(ship.RotationAxis, rot_speed * deltaTime));
//                
//                pos.Value += deltaTime * fwd_speed *
//                           math.mul(rot.Value, new float3(0, 0, 1));
//            }
//        }
//
//        protected override JobHandle OnUpdate(JobHandle inputDeps){
//           return new MoveJob
//           {
//               deltaTime = Time.deltaTime,
//               rot_speed = 1f,
//               fwd_speed = 1f
//           }.Schedule(this, inputDeps);
//        }
//
//
//    }
//}
