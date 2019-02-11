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
                sens.Position = (pos.Value - center + ship.TargetOffset);
                sens.Rotation = rot.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps){
           return new SensorJob{center = Center}.Schedule(this, inputDeps);
        }

//        private ComponentGroup _shipComponentGroup;
//        private ComponentGroup _targetComponentGroup;
//        
//        protected override void OnCreateManager()
//        {
//            _targetComponentGroup = GetComponentGroup(
//                ComponentType.ReadOnly(typeof(Position)),
//                ComponentType.ReadOnly(typeof(TargetShip))
//            );
//            _shipComponentGroup = GetComponentGroup(
//                ComponentType.ReadOnly(typeof(Position)), 
//                typeof(ShipSensor),
//                ComponentType.ReadOnly(typeof(Ship)));
//
//        }
        


    }



}
