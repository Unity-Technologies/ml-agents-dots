using System;
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
    public class ImpactSystem : JobComponentSystem
    {
        public float3 Center;
        public float Radius;

        private BeginInitializationEntityCommandBufferSystem _entityCommandBufferSystem;
        
        private struct ImpactJob : IJobForEachWithEntity<Translation, Scale>
        {
            public EntityCommandBuffer.Concurrent buffer;
           
            public float3 Center;
            public float Radius;
            public void Execute(Entity entity, int i, [ReadOnly] ref Translation position, ref Scale scale)
            {
                var distSquared = math.dot(position.Value - Center, position.Value - Center);
                if (distSquared < Radius * Radius)
                {
                    buffer.DestroyEntity(i, entity);
                    var newEntity = buffer.CreateEntity(i);
                    buffer.AddSharedComponent<RenderMesh>(i, newEntity, Globals.ExplosionRenderer);
                    buffer.AddComponent<Translation>(i, newEntity, position);
                    buffer.AddComponent<Scale>(i, newEntity, new Scale{Value = 1.0f});
                    buffer.AddComponent<Explosion>(i, newEntity, new Explosion
                    {
                        TimeSinceBirth = 0f,
                        DeathTime = 0.5f,
                        GrowthRate = 10f
                    });
                    buffer.AddComponent(i, newEntity, new LocalToWorld());

                }
                
            }
        }
        
        private struct LateExplosionJob : IJobForEachWithEntity<Scale, Explosion>
        {
            public EntityCommandBuffer.Concurrent buffer;
            public float deltaTime;
            public void Execute(Entity entity, int i, ref Scale scale, ref Explosion explosion)
            {
                explosion.TimeSinceBirth = explosion.TimeSinceBirth + deltaTime;
                scale.Value = explosion.TimeSinceBirth * explosion.GrowthRate + 1;
                if (explosion.TimeSinceBirth > explosion.DeathTime)
                {
                    buffer.DestroyEntity(i, entity);
                }
            }
        }
        
        
        protected override void OnCreate()
        {
            _entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            var destroyJob = new ImpactJob
            {
                buffer = _entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                Center = Center,
                Radius = Radius
            };

            var exploJob = new LateExplosionJob
            {
                buffer = _entityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                deltaTime = Time.deltaTime
            };

            var handle = destroyJob.Schedule(this, inputDeps);
            _entityCommandBufferSystem.AddJobHandleForProducer(handle);
            handle.Complete();
            handle = exploJob.Schedule(this, handle);
            _entityCommandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}