using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;



namespace DOTS_MLAgents.Example.SpaceWars.Scripts
{
    public class DeleteRogueBarrier : EntityCommandBufferSystem{}
    public class ShootBarrier : EntityCommandBufferSystem{}
    
    public class ShipPhysics : JobComponentSystem
    {
#pragma warning disable 0649
        private DeleteRogueBarrier rogueBarrier;
        private ShootBarrier shootBarrier;
#pragma warning restore 0649
        private EntityQuery _positionComponentGroup;
        private EntityQuery _shipComponentGroup;
        
        protected override void OnCreateManager()
        {
            rogueBarrier = World.GetOrCreateSystem<DeleteRogueBarrier>();
            shootBarrier = World.GetOrCreateSystem<ShootBarrier>();

            _positionComponentGroup = GetEntityQuery(
                ComponentType.ReadOnly(typeof(Translation)),
                ComponentType.ReadOnly(typeof(Projectile))
                );
            _shipComponentGroup = GetEntityQuery(
                typeof(Translation), 
                typeof(Rotation), 
                ComponentType.ReadOnly(typeof(Steering)),
                typeof(Ship));

        }
        
        private struct MovementJob : IJobParallelFor
        {
            public float deltaTime;
            public NativeArray<Translation> positions;
            public NativeArray<Rotation> rotations;
            public NativeArray<Ship> ships;
            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<Steering> steerings;
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
                    var ent = buffer.CreateEntity(i);
                    buffer.AddSharedComponent(i, ent, Globals.ProjectileRenderer);
                    buffer.AddComponent(i, ent, positions[i]);
                    buffer.AddComponent(i, ent, rotations[i]);
                    buffer.AddComponent(i, ent, new Scale
                    {
                        Value = Globals.PROJECTILE_SCALE
                    });
                    buffer.AddComponent(i, ent, new Projectile());
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
            [ReadOnly] public NativeArray<Translation> positions;
            [DeallocateOnJobCompletion]
            [ReadOnly] public NativeArray<Entity> entities;
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

        private struct ProjectileMovement : IJobForEach<
            Translation, Rotation, Projectile>
        {
            public float deltaTime;
            public void Execute(ref Translation pos, ref Rotation rot, ref Projectile proj)
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
                positions = _shipComponentGroup.ToComponentDataArray<Translation>(Allocator.TempJob),
                rotations = _shipComponentGroup.ToComponentDataArray<Rotation>(Allocator.TempJob),
                steerings =  _shipComponentGroup.ToComponentDataArray<Steering>(Allocator.TempJob),
                ships = _shipComponentGroup.ToComponentDataArray<Ship>(Allocator.TempJob)
            };
            
            var destroyjob = new DestroyRogue
            {
                buffer = rogueBarrier.CreateCommandBuffer().ToConcurrent(),
                positions = moveJob.positions,//_positionComponentGroup.ToComponentDataArray<Translation>(Allocator.TempJob),
                entities = _positionComponentGroup.ToEntityArray(Allocator.TempJob)
            };
            var projectileJob = new ProjectileMovement
            {
                deltaTime = Time.deltaTime,
            };
            var handle = moveJob.Schedule(_shipComponentGroup.CalculateLength(), 64, inputDeps);
            shootBarrier.AddJobHandleForProducer(handle);
            handle = destroyjob.Schedule(_positionComponentGroup.CalculateLength(), 64, handle);
            rogueBarrier.AddJobHandleForProducer(handle);
            handle = projectileJob.Schedule(this, handle);
            _shipComponentGroup.CopyFromComponentDataArray<Translation>(destroyjob.positions);
            _shipComponentGroup.CopyFromComponentDataArray<Rotation>(moveJob.rotations);
            _shipComponentGroup.CopyFromComponentDataArray<Ship>(moveJob.ships);
            destroyjob.positions.Dispose();
            moveJob.rotations.Dispose();
            moveJob.ships.Dispose();
            // TODO : There has to be a simpler way to do this.
            return handle;
        }
    }
}
