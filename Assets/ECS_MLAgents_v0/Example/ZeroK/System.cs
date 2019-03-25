using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Unity.Jobs;
using Random = Unity.Mathematics.Random;
using RandomS = UnityEngine.Random;

namespace ECS_MLAgents_v0.Example.ZeroK.Scripts
{
    /// <summary>
    /// Handles the physics of the Spheres. The position is updated at each update based of the
    /// velocity of the sphere, the velocity of the spheres is updated based on their acceleration,
    /// and the sphere that are too far off are reset to the center.
    /// </summary>
    [UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
    public class MovementSystem : JobComponentSystem
    {
        private struct MovementJob : IJobProcessComponentData<Position, Speed>
        {
            public float deltaTime;
    
            public void Execute(
                ref Position position, ref Speed speed)
            {
                position.Value += 0.2f * speed.Value;

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
                    RandomS.Range(-1f, 1f),
                    RandomS.Range(-1f, 1f),
                    RandomS.Range(-1f, 1f))
            };
            var handle =  moveJob.Schedule(this, inputDeps);
            return resetJob.Schedule(this, handle);
        }
    }   
    
    [UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
    public class PopulateSensor : JobComponentSystem
    {
        private struct PopulateJob : IJobProcessComponentData<Sensor, Position>
        {
    
            public void Execute(
                ref Sensor sensor, ref Position pos)
            {
                sensor.Position = pos.Value / 50;
                sensor.Timer += 0.001f;
                // sensor.Timer += 0.01f;
                if (sensor.Timer > 1f)
                {
                    sensor.Done = 1f;
                }
//                sensor.Reward = math.min(0.1f, 1f / math.length(pos.Value));
                sensor.Reward = 0.1f * math.min(1f , 1f / math.length(pos.Value)) - 0.05f;

            }
        }

        private struct ResetPositionsJob : IJobProcessComponentData<Position, Sensor>
        {

            public Random random;
            public void Execute(ref Position position, ref Sensor sensor)
            {
                if (sensor.Timer > 1.0015f)
                // if (sensor.Timer > 1.02f)
                {
                    sensor.Done = 0;
                    sensor.Timer = 0;
                    position.Value = random.NextFloat3(20 * new float3(1,1,1)) - 10 * new float3(1,1,1);

                }
            }
        }
    
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveJob = new PopulateJob
            {
            };
            var resetJob = new ResetPositionsJob{
                random = new Random((uint)RandomS.Range(1, 100000))
            };
            var handle =  moveJob.Schedule(this, inputDeps);
            return resetJob.Schedule(this, handle);
        }
    }  
    
}
