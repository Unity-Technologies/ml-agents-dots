using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.AI.MLAgents;
using Unity.Burst;


/// <summary>
/// This IComponentData hold the velocity of each sphere.
/// </summary>
public struct Speed : IComponentData
{
    public float3 Value;
}

/// <summary>
/// This component will represent the acceleration of the spheres
/// </summary>
public struct Acceleration : IComponentData
{
    public float3 Value;
}

/// <summary>
/// Handles the physics of the Spheres. The position is updated at each update based of the
/// velocity of the sphere, the velocity of the spheres is updated based on their acceleration,
/// and the sphere that are too far off are reset to the center.
/// </summary>
// [DisableAutoCreation]
public class SpaceMagicMovementSystem : JobComponentSystem
{

    // [BurstCompile]
    private struct AccelerateJob : IActuatorJob
    {
        public ComponentDataFromEntity<Acceleration> ComponentDataFromEntity;
        public void Execute(ActuatorEvent ev)
        {
            var a = new Acceleration();
            ev.GetContinuousAction(out a);
            ComponentDataFromEntity[ev.Entity] = a;
        }
    }
    // [BurstCompile]
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
            // Debug.Log("RequestDecision");
        }
    }

    [BurstCompile]
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

    protected override void OnCreate()
    {
        var sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
        world = new MLAgentsWorld(10001, ActionType.CONTINUOUS, new int3[] { new int3(3, 0, 0) }, 3);
        sys.SubscribeWorldWithHeuristic("SpaceMagic", world, () =>
        {
            if (Input.GetKey(KeyCode.Space))
            {
                return new float3(-10, -10, -10);
            }
            else
            {
                return new float3();
            }
        }
        );

        // var world2 = new MLAgentsWorld(10, ActionType.DISCRETE, new int3[] { new int3(5, 0, 0) }, 6, new int[6] { 2, 3, 4, 5, 6, 7 });
        // sys.SubscribeWorld("SpaceMagic2", world2);

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var accJob = new AccelerateJob
        {
            ComponentDataFromEntity = GetComponentDataFromEntity<Acceleration>(isReadOnly: false)
        };

        var moveJob = new MovementJob
        {
            w = world,
            deltaTime = Time.DeltaTime
        };

        var resetJob = new ResetPositionsJob
        {
            initialPosition = new float3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f))
        };

        inputDeps = accJob.Schedule(world, inputDeps);

        inputDeps = moveJob.Schedule(this, inputDeps);
        inputDeps = resetJob.Schedule(this, inputDeps);
        inputDeps.Complete();
        return inputDeps;
    }

    protected override void OnDestroy()
    {
        world.Dispose();

    }
}


