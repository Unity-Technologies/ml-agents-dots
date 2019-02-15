using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;



namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    public class DeleteRogueBarrier : BarrierSystem{}
    public class ShootBarrier : BarrierSystem{}
    
    public class ShipPhysics : JobComponentSystem
    {
#pragma warning disable 0649
        [Inject] private DeleteRogueBarrier rogueBarrier;
        [Inject] private ShootBarrier shootBarrier;
#pragma warning restore 0649
        private ComponentGroup _positionComponentGroup;
        private ComponentGroup _shipComponentGroup;
        
        protected override void OnCreateManager()
        {
            _positionComponentGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(Position)),
                ComponentType.ReadOnly(typeof(Projectile))
                );
            _shipComponentGroup = GetComponentGroup(
                typeof(Position), 
                typeof(Rotation), 
                ComponentType.ReadOnly(typeof(Steering)),
                typeof(Ship));

        }
        
        private struct MovementJob : IJobParallelFor
        {
     
            public float deltaTime;
            
            public ComponentDataArray<Position> positions;
            public ComponentDataArray<Rotation> rotations;
            public ComponentDataArray<Ship> ships;
            [ReadOnly] public ComponentDataArray<Steering> steerings;
            [ReadOnly] public EntityCommandBuffer.Concurrent buffer;
    
            public void Execute(int i)
            {
                var r = rotations[i];
                var s = steerings[i];
                var p = positions[i];
                var ship = ships[i];
                r.Value = math.mul(
                    r.Value,
                    quaternion.AxisAngle(math.up(), Globals.SHIP_ROTATION_SPEED * s.YAxis * deltaTime));
                r.Value = math.mul(
                    r.Value,
                    quaternion.AxisAngle(new float3(1, 0, 0), Globals.SHIP_ROTATION_SPEED  * s.XAxis * deltaTime));
                p.Value += deltaTime * Globals.SHIP_SPEED *
                           math.mul(r.Value, new float3(0, 0, 1));



                if (steerings[i].Shoot > 0.5f)
                {
                    ship.Fire = 1;
                }
                if (ship.Fire == 1 && ship.ReloadTime < 0)
                {
                    ship.Fire = 0;
                    buffer.CreateEntity(i);
                    buffer.AddSharedComponent(i, Globals.ProjectileRenderer);
                    buffer.AddComponent(i, positions[i]);
                    buffer.AddComponent(i, rotations[i]);
                    buffer.AddComponent (i, new Scale
                    {
                        Value = new float3(
                            Globals.PROJECTILE_SCALE_X,
                            Globals.PROJECTILE_SCALE_Y,
                            Globals.PROJECTILE_SCALE_Z
                        )
                    });
                    buffer.AddComponent(i, new Projectile());
                }

                if (ship.ReloadTime < 0)
                {
                    ship.ReloadTime = ship.MaxReloadTime;
                }

                ship.ReloadTime -= deltaTime;
                rotations[i] = r;
                positions[i] = p;
                ships[i] = ship;
            }
        }
        
        private struct DestroyRogue : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent buffer;
            [ReadOnly] public ComponentDataArray<Position> positions;
            [ReadOnly] public EntityArray entities;
            public void Execute(int i)
            {
                if (positions[i].Value.x * positions[i].Value.x +
                    positions[i].Value.y * positions[i].Value.y +
                    positions[i].Value.z * positions[i].Value.z > Globals.BOUNDARIES)
                {
                    buffer.DestroyEntity(i, entities[i]);
                }
                
            }
        }

        private struct ProjectileMovement : IJobProcessComponentData<
            Position, Rotation, Projectile>
        {
            public float deltaTime;
            public void Execute(ref Position pos, ref Rotation rot, ref Projectile proj)
            {
                pos.Value += deltaTime * Globals.PROJECTILE_SPEED * 
                           math.mul(rot.Value, new float3(0, 0, 1));
            }
        }
        

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            var moveJob = new MovementJob
            {
                deltaTime = Time.deltaTime,
                buffer = shootBarrier.CreateCommandBuffer().ToConcurrent(),
                positions = _shipComponentGroup.GetComponentDataArray<Position>(),
                rotations = _shipComponentGroup.GetComponentDataArray<Rotation>(),
                steerings =  _shipComponentGroup.GetComponentDataArray<Steering>(),
                ships = _shipComponentGroup.GetComponentDataArray<Ship>()
            };
            
            var destroyjob = new DestroyRogue
            {
                buffer = rogueBarrier.CreateCommandBuffer().ToConcurrent(),
                positions = _positionComponentGroup.GetComponentDataArray<Position>(),
                entities = _positionComponentGroup.GetEntityArray()
            };
            var projectileJob = new ProjectileMovement
            {
                deltaTime = Time.deltaTime,
            };
            var handle = moveJob.Schedule(_shipComponentGroup.CalculateLength(), 64, inputDeps);
            handle = destroyjob.Schedule(_positionComponentGroup.CalculateLength(), 64, handle);
            handle = projectileJob.Schedule(this, handle);
            return handle;
        }
    }
}
