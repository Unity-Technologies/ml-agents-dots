using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.AI.MLAgents;
using Unity.Burst;
using Unity.Collections;
using Unity.Physics;


public struct BallResetData : IComponentData
{
    public float3 ResetPosition;
}
public struct RefToPlatform : IComponentData
{
    public Entity Value;
}

public struct BallData : IComponentData
{
    public bool WasDropped;
    public float3 position;
}

public struct AngularAcceleration : IComponentData
{
    public float2 Value;
}

public class BallSystem : JobComponentSystem
{
    private struct RotateJob : IActuatorJob
    {
        public ComponentDataFromEntity<AngularAcceleration> ComponentDataFromEntity;
        public void Execute(ActuatorEvent ev)
        {
            var a = new AngularAcceleration();
            ev.GetContinuousAction(out a);
            ComponentDataFromEntity[ev.Entity] = a;
        }
    }


    private struct BallDropped : IJobForEach<RefToPlatform, Translation, BallResetData, PhysicsVelocity>
    {
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<BallData> ComponentDataFromEntity;
        public void Execute(ref RefToPlatform platform, ref Translation translation, ref BallResetData resetData, ref PhysicsVelocity vel)
        {
            var dropped = (translation.Value.y < resetData.ResetPosition.y - 0.7f);
            ComponentDataFromEntity[platform.Value] = new BallData
            {
                WasDropped = ComponentDataFromEntity[platform.Value].WasDropped || dropped,
                position = translation.Value
            };
            if (dropped)
            {
                vel.Linear = new float3();
                vel.Angular = new float3();
                translation.Value = resetData.ResetPosition;
            }
        }
    }

    private struct MovePlatform : IJobForEachWithEntity<Translation, Rotation, AngularAcceleration, BallData>
    {
        public MLAgentsWorld world;
        public bool RequestDecision;
        public void Execute(Entity entity, int i, ref Translation trans, ref Rotation rotation, ref AngularAcceleration acc, ref BallData droppedTag)
        {
            acc.Value.x = math.clamp(acc.Value.x, -1, 1);
            acc.Value.y = math.clamp(acc.Value.y, -1, 1);
            var rot = math.mul(rotation.Value, quaternion.Euler(0.05f * new float3(acc.Value.x, 0, acc.Value.y)));
            rotation.Value = rot;
            if (RequestDecision)
            {
                world.RequestDecision(entity)
                    .SetObservation(0, rotation.Value)
                    .SetObservation(1, droppedTag.position - trans.Value)
                    .HasTerminated(droppedTag.WasDropped, false) // Are you sure ?
                    .SetReward((droppedTag.WasDropped ? -1f : 0.1f));// - 0.025f * (math.abs(acc.Value.x) + math.abs(acc.Value.y)));

                if (droppedTag.WasDropped)
                {
                    droppedTag.WasDropped = false;
                    rotation = new Rotation
                    {
                        Value = quaternion.EulerXYZ(0, 0, 0)
                    };
                }
            }
        }
    }

    public MLAgentsWorld world;
    int counter;


    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        inputDeps = new BallDropped
        {
            ComponentDataFromEntity = GetComponentDataFromEntity<BallData>(isReadOnly: false)
        }.Schedule(this, inputDeps);

        if (!world.IsCreated){
            return inputDeps;
        }

        var senseJob = new MovePlatform
        {
            world = world,
            RequestDecision = counter % 5 == 0,
        };
        inputDeps = senseJob.Schedule(this, inputDeps);

        var reactiveJob = new RotateJob
        {
            ComponentDataFromEntity = GetComponentDataFromEntity<AngularAcceleration>(isReadOnly: false)
        };
        inputDeps = reactiveJob.Schedule(world, inputDeps);


        counter++;
        // sys.RegisterDependency(inputDeps);
        return inputDeps;
    }

    protected override void OnDestroy()
    {
        world.Dispose();
    }
}
