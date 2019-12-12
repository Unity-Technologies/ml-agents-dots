using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using DOTS_MLAgents.Core;

namespace DOTS_MLAgents.Example.SpaceMagic.Scripts
{
    /// <summary>
    /// Handles the physics of the Spheres. The position is updated at each update based of the
    /// velocity of the sphere, the velocity of the spheres is updated based on their acceleration,
    /// and the sphere that are too far off are reset to the center.
    /// </summary>
    public class SpaceMagicMovementSystem : JobComponentSystem
    {

        private struct AccelerateJob : IActuatorJob
        {
            public EntityCommandBuffer ECB;
            public void Execute(ActuatorEvent ev)
            {
                var a = new Acceleration();
                ev.GetContinuousAction(out a);
                ECB.SetComponent(ev.Entity, a);
            }
        }
        private struct MovementJob : IJobForEachWithEntity<Translation, Speed, Acceleration>
        {
            public float deltaTime;
            public MLAgentsWorld w;

            public void Execute(
                Entity entity,
                int i,
                ref Translation position,
                ref Speed speed,
                ref Acceleration acceleration)
            {
                position.Value += deltaTime * speed.Value;
                speed.Value += deltaTime * (acceleration.Value - 0.05f * speed.Value);
                w.RequestDecision(entity).SetObservation(0, position);
            }
        }

        private struct ResetPositionsJob : IJobForEach<Translation, Speed>
        {
            public float3 initialPosition;
            public void Execute(ref Translation position, ref Speed speed)
            {
                if (position.Value.x * position.Value.x +
                    position.Value.y * position.Value.y +
                    position.Value.z * position.Value.z > 1e6)
                {
                    position.Value = initialPosition;
                    speed.Value = 10 * initialPosition;
                }
            }
        }

        MLAgentsWorld world;
        private EndSimulationEntityCommandBufferSystem timeBarrier;

        protected override void OnCreate()
        {
            var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
            world = new MLAgentsWorld(1001, ActionType.CONTINUOUS, new int3[] { new int3(3, 0, 0) }, 3);
            sys.SubscribeWorld("SpaceMagic", world);

            // var world2 = new MLAgentsWorld(10, ActionType.DISCRETE, new int3[] { new int3(5, 0, 0) }, 6, new int[6] { 2, 3, 4, 5, 6, 7 });
            // sys.SubscribeWorld("SpaceMagic2", world2);
            timeBarrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var accJob = new AccelerateJob
            {
                ECB = timeBarrier.CreateCommandBuffer()
            };


            var moveJob = new MovementJob
            {
                w = world,
                deltaTime = Time.deltaTime
            };

            var resetJob = new ResetPositionsJob
            {
                initialPosition = new float3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f))
            };

            inputDeps = accJob.Schedule(world, inputDeps);
            inputDeps.Complete();
            inputDeps = moveJob.Schedule(this, inputDeps);
            inputDeps = resetJob.Schedule(this, inputDeps);
            return inputDeps;
        }
    }
}

