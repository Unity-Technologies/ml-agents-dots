using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    [Serializable]
    public struct ShipSensor : IComponentData
    {
//        public float Reward;
        public float3 Position;
        public quaternion Rotation;
    }
    
    public class ShipSensorComponent: ComponentDataWrapper<ShipSensor> {}

    public class SensorPopulate : JobComponentSystem
    {
        public float3 Center;
        
        private struct SensorJob : IJobProcessComponentData<
            Position, Rotation, ShipSensor, Ship>
        {

            public float3 center;
            public void Execute(
                ref Position pos, 
                ref Rotation rot, 
                ref ShipSensor sens,
                ref Ship ship)
            {
                var tmpPos = pos.Value - center + ship.TargetOffset;
                if (tmpPos.x * tmpPos.x < 0.00001f)
                {
                    tmpPos = new float3(0,0,1);
                }
                sens.Position = math.normalize(tmpPos);
                sens.Rotation = rot.Value;
//                sens.Reward = GenerateReward(sens.Position, sens.Rotation, ship.Fire);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps){
           return new SensorJob{center = Center}.Schedule(this, inputDeps);
        }

        private static float GenerateReward(float3 pos, quaternion rot, int fire)
        {
            var forwardRelativeRef = math.mul(rot, new float3(0, 0, 1));

            return -math.dot(forwardRelativeRef, pos);//+ fire;
        }
    }
}
