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
    public class ShipPhysics : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem _entityCommandBufferSystem;
        
        protected override void OnCreate()
        {
            _entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        
        private struct MovementJob : IJobForEachWithEntity<Translation, Rotation, Ship, Steering>
        {
     
            public float deltaTime;
            
            [ReadOnly] public EntityCommandBuffer.Concurrent buffer;
    
            public void Execute(Entity entity, int i, ref Translation position, ref Rotation rotation, ref Ship ship, ref Steering steering)
            {
                var r = rotation;
                var s = steering;
                var p = position;
                
                rotation.Value = math.mul(
                    rotation.Value,
                    quaternion.AxisAngle(math.up(), Globals.SHIP_ROTATION_SPEED * steering.YAxis * deltaTime));
                rotation.Value = math.mul(
                    rotation.Value,
                    quaternion.AxisAngle(new float3(1, 0, 0), Globals.SHIP_ROTATION_SPEED  * steering.XAxis * deltaTime));
                position.Value += deltaTime * Globals.SHIP_SPEED *
                           math.mul(rotation.Value, new float3(0, 0, 1));

                if (steering.Shoot > 0.5f)
                {
                    ship.Fire = 1;
                }
                if (ship.Fire == 1 && ship.ReloadTime < 0)
                {
                    ship.Fire = 0;
                    var newEntity = buffer.CreateEntity(i);
                    buffer.AddSharedComponent<RenderMesh>(i, newEntity, Globals.ProjectileRenderer);
                    buffer.AddComponent(i, newEntity, position);
                    buffer.AddComponent(i, newEntity, rotation);
                    buffer.AddComponent(i, newEntity, new Scale { Value = Globals.PROJECTILE_SCALE });
                    buffer.AddComponent(i, newEntity, new Projectile());
                    buffer.AddComponent(i, newEntity, new LocalToWorld());
                }

                if (ship.ReloadTime < 0)
                {
                    ship.ReloadTime = ship.MaxReloadTime;
                }

                ship.ReloadTime -= deltaTime;
            }
        }
        
        private struct DestroyRogue : IJobForEachWithEntity<Translation>
        {
            public EntityCommandBuffer.Concurrent buffer;
            
            public void Execute(Entity entity, int i, [ReadOnly] ref Translation position)
            {
                if (position.Value.x * position.Value.x +
                    position.Value.y * position.Value.y +
                    position.Value.z * position.Value.z > Globals.BOUNDARIES)
                {
                    buffer.DestroyEntity(i, entity);
                }
                
            }
        }

        private struct ProjectileMovement : IJobForEach<Translation, Rotation, Projectile>
        {
            public float deltaTime;
            public void Execute(ref Translation position, ref Rotation rotation, ref Projectile projectile)
            {
                position.Value += deltaTime * Globals.PROJECTILE_SPEED * 
                           math.mul(rotation.Value, new float3(0, 0, 1));
            }
        }
        

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            var moveJob = new MovementJob
            {
                deltaTime = Time.deltaTime,
                buffer = _entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            };
            
            var destroyjob = new DestroyRogue
            {
                buffer = _entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            };
            var projectileJob = new ProjectileMovement
            {
                deltaTime = Time.deltaTime,
            };
            
            var handle = moveJob.Schedule(this, inputDeps);
            handle = destroyjob.Schedule(this, handle);
            handle = projectileJob.Schedule(this, handle);
            return handle;
        }
    }
}