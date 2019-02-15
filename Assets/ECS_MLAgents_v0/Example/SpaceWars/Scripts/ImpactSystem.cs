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

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    public class ImpactBarrier : BarrierSystem{}
    public class LateExplosionBarrier : BarrierSystem{}
    
    public class ImpactSystem : JobComponentSystem
    {
        public float3 Center;
        public float Radius;
        
#pragma warning disable 0649
        [Inject] private ImpactBarrier impactBarrier;
        [Inject] private LateExplosionBarrier lateExplosionBarrier;
#pragma warning restore 0649
        private ComponentGroup _positionComponentGroup;
        private ComponentGroup _explosionComponentGroup;
        
        private struct ImpactJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent buffer;
            [ReadOnly] public ComponentDataArray<Position> positions;
            [ReadOnly] public EntityArray entities;
            public float3 Center;
            public float Radius;
            public void Execute(int i)
            {
                var distSquared = math.dot(positions[i].Value - Center, positions[i].Value - Center);
                if (distSquared < Radius * Radius)
                {
                    buffer.DestroyEntity(i, entities[i]);
                    buffer.CreateEntity(i);
                    buffer.AddSharedComponent(i, Globals.ExplosionRenderer);
                    buffer.AddComponent(i, positions[i]);
                    buffer.AddComponent(i, new Scale{Value = new float3(1,1,1)});
                    buffer.AddComponent(i, new Explosion
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
            public ComponentDataArray<Explosion> explosions;
            public ComponentDataArray<Scale> scales;
            [ReadOnly] public EntityArray entities;
            public float deltaTime;
            public void Execute(int i)
            {
                var explo = explosions[i];
                explo.TimeSinceBirth = explo.TimeSinceBirth + deltaTime;
                var scale = scales[i];
                scale.Value = new float3(1,1,1) * explo.TimeSinceBirth * explo.GrowthRate + 1;
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
            _positionComponentGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(Position)),
                ComponentType.ReadOnly(typeof(Projectile))
            );
            _explosionComponentGroup = GetComponentGroup(
                typeof(Explosion),
                typeof(Scale)
            );
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            
            var destroyjob = new ImpactJob
            {
                buffer = impactBarrier.CreateCommandBuffer().ToConcurrent(),
                positions = _positionComponentGroup.GetComponentDataArray<Position>(),
                entities = _positionComponentGroup.GetEntityArray(),
                Center = Center,
                Radius = Radius
            };

            var exploJob = new LateExplosionJob
            {
                buffer = lateExplosionBarrier.CreateCommandBuffer().ToConcurrent(),
                explosions = _explosionComponentGroup.GetComponentDataArray<Explosion>(),
                scales = _explosionComponentGroup.GetComponentDataArray<Scale>(),
                entities = _explosionComponentGroup.GetEntityArray(),
                deltaTime = Time.deltaTime
            };

            var handle = inputDeps;
            handle = destroyjob.Schedule(_positionComponentGroup.CalculateLength(), 64, handle);
            handle = exploJob.Schedule(_explosionComponentGroup.CalculateLength(), 64, handle);
            return handle;
        }
    }
}
