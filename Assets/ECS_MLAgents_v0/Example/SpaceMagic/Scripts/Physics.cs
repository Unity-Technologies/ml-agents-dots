using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS_MLAgents_v0.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// Handles the physics of the Spheres. The position is updated at each update based of the
    /// velocity of the sphere, the velocity of the spheres is updated based on their acceleration,
    /// and the sphere that are too far off are reset to the center.
    /// </summary>
    public class MovementSystem : JobComponentSystem
    {
        private struct MovementJob : IJobProcessComponentData<Position, Speed, Acceleration>
        {
            public float deltaTime;
    
            public void Execute(
                ref Position position, ref Speed speed, ref Acceleration acceleration)
            {
                position.Value += deltaTime * speed.Value;
                speed.Value += deltaTime * (acceleration.Value - 0.05f * speed.Value);
            }
        }

        private struct ResetPositionsJob : IJobProcessComponentData<Position, Speed>
        {
            public float3 initialPosition;
            public void Execute(ref Position position, ref Speed speed)
            {
                if (position.Value.x * position.Value.x +
                    position.Value.y * position.Value.y +
                    position.Value.z * position.Value.z > 1e6)
                {
                    position.Value = initialPosition;
                    speed.Value = 10*initialPosition;
                }
            }
        }
    
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveJob = new MovementJob
            {
                deltaTime = Time.deltaTime
            };
            var resetJob = new ResetPositionsJob{
                initialPosition = new float3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f))
                };
            var handle =  moveJob.Schedule(this, inputDeps);
            return resetJob.Schedule(this, handle);
        }
    }   
}

