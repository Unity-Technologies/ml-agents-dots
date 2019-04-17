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
    public class ImpactBarrier : EntityCommandBufferSystem{}
    public class LateExplosionBarrier : EntityCommandBufferSystem{}
    
    public class ImpactSystem : JobComponentSystem
    {
        public float3 Center;
        public float Radius;
        
#pragma warning disable 0649
        private ImpactBarrier impactBarrier;
        private LateExplosionBarrier lateExplosionBarrier;
#pragma warning restore 0649
        private EntityQuery _positionComponentGroup;
        private EntityQuery _explosionComponentGroup;
        
        private struct ImpactJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent buffer;
            [ReadOnly] public NativeArray<Translation> positions;
            [ReadOnly] public NativeArray<Entity> entities;
            public float3 Center;
            public float Radius;
            public void Execute(int i)
            {
                var distSquared = math.dot(positions[i].Value - Center, positions[i].Value - Center);
                if (distSquared < Radius * Radius)
                {
                    buffer.DestroyEntity(i, entities[i]);
                    buffer.CreateEntity(i);
                    buffer.AddSharedComponent(i, entities[i], Globals.ExplosionRenderer);
                    buffer.AddComponent(i, entities[i],positions[i]);
                    buffer.AddComponent(i, entities[i], new Scale{Value = 1f});
                    buffer.AddComponent(i, entities[i], new Explosion
                    {
                        TimeSinceBirth = 0f,
                        DeathTime = 0.5f,
                        GrowthRate = 10f
                    });

                }
                
            }
        }
        
        private struct LateExplosionJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent buffer;
            public NativeArray<Explosion> explosions;
            public NativeArray<Scale> scales;
            [ReadOnly] public NativeArray<Entity> entities;
            public float deltaTime;
            public void Execute(int i)
            {
                var explo = explosions[i];
                explo.TimeSinceBirth = explo.TimeSinceBirth + deltaTime;
                var scale = scales[i];
                scale.Value = explo.TimeSinceBirth * explo.GrowthRate + 1;
                scales[i] = scale;
                explosions[i] = explo;
                if (explo.TimeSinceBirth > explo.DeathTime)
                {
                    buffer.DestroyEntity(i, entities[i]);
                }
            }
        }
        
        
        protected override void OnCreateManager()
        {
            impactBarrier = World.GetOrCreateSystem<ImpactBarrier>();
            lateExplosionBarrier = World.GetOrCreateSystem<LateExplosionBarrier>();
            _positionComponentGroup = GetEntityQuery(
                ComponentType.ReadOnly(typeof(Translation)),
                ComponentType.ReadOnly(typeof(Projectile))
            );
            _explosionComponentGroup = GetEntityQuery(
                typeof(Explosion),
                typeof(Scale)
            );
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            var destroyjob = new ImpactJob
            {
                buffer = impactBarrier.CreateCommandBuffer().ToConcurrent(),
                positions = _positionComponentGroup.ToComponentDataArray<Translation>(Allocator.TempJob),
                entities = _positionComponentGroup.ToEntityArray(Allocator.TempJob),
                Center = Center,
                Radius = Radius
            };

            var exploJob = new LateExplosionJob
            {
                buffer = lateExplosionBarrier.CreateCommandBuffer().ToConcurrent(),
                explosions = _explosionComponentGroup.ToComponentDataArray<Explosion>(Allocator.TempJob),
                scales = _explosionComponentGroup.ToComponentDataArray<Scale>(Allocator.TempJob),
                entities = _explosionComponentGroup.ToEntityArray(Allocator.TempJob),
                deltaTime = Time.deltaTime
            };

            var handle = inputDeps;
            handle = destroyjob.Schedule(_positionComponentGroup.CalculateLength(), 64, handle);
            handle = exploJob.Schedule(_explosionComponentGroup.CalculateLength(), 64, handle);
            return handle;
        }
    }
}
