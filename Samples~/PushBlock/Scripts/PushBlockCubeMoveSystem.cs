using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.AI.MLAgents;



[UpdateBefore(typeof(BlockCollisionSystem))]
public class PushBlockCubeMoveSystem : JobComponentSystem
{
    public Policy PushBlockPolicy;
    Unity.Physics.Systems.BuildPhysicsWorld physicsWorldSystem;
    int counter = 0;

    private struct UpdatePushBlockAction : IActuatorJob
    {
        public ComponentDataFromEntity<PushBlockAction> ComponentDataFromEntity;
        public void Execute(ActuatorEvent ev)
        {
            var a = ev.GetDiscreteAction<PushBlockAction>();
            ComponentDataFromEntity[ev.Entity] = a;
        }
    }

    protected override void OnCreate()
    {
        physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!PushBlockPolicy.IsCreated){return inputDeps;}
        inputDeps.Complete();

        var positionData = GetComponentDataFromEntity<Translation>(isReadOnly : false);
        var policy = PushBlockPolicy;


        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

        int NumOfRayCasts = 9;

        counter ++;
        if (counter % 8 == 0)
        inputDeps = Entities.WithReadOnly(collisionWorld).WithNativeDisableContainerSafetyRestriction(positionData).ForEach((Entity entity, ref PushBlockCube cube, ref Rotation rot) => {

            var pos = positionData[entity];






            /* RAYCAST SECTION*/
            var Rinputs = new NativeArray<Unity.Physics.RaycastInput>(NumOfRayCasts, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var hit = new NativeList<Unity.Physics.RaycastHit>(8, Allocator.Temp);
            var allRayData = new NativeArray<float>(NumOfRayCasts * 2 * 4, Allocator.Temp, NativeArrayOptions.ClearMemory);


            // Collide with all level 0
            var collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u, // all 1s, so all layers, collide with everything
                CollidesWith = ~0u,
                GroupIndex = 0
            };
            PrepareRayCastInputs(ref Rinputs, ref pos, ref rot, ref collisionFilter, 0f);
            for (int i = 0; i < NumOfRayCasts; i++)
            {
                hit.Clear();
                bool haveHit = collisionWorld.CastRay(Rinputs[i], ref hit);
                float minDistance = 1.0f;

                if (haveHit)
                {

                    for (int j = 0; j < hit.Length; j++)
                    {
                        if (hit[j].Fraction > 0.001f) // Seems when ray cast starts in the collider, fraction is 0.0f;
                        {
                            minDistance = math.min(minDistance, hit[j].Fraction);
                        }
                    }
                }
                allRayData[i * 8] = minDistance;
            }
            // Collide with all level 1
             collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u, // all 1s, so all layers, collide with everything
                CollidesWith = ~0u,
                GroupIndex = 0
            };
            PrepareRayCastInputs(ref Rinputs, ref pos, ref rot, ref collisionFilter, 1f);
            for (int i = 0; i < NumOfRayCasts; i++)
            {
                hit.Clear();
                bool haveHit = collisionWorld.CastRay(Rinputs[i], ref hit);
                float minDistance = 1.0f;

                if (haveHit)
                {

                    for (int j = 0; j < hit.Length; j++)
                    {
                        if (hit[j].Fraction > 0.001f) // Seems when ray cast starts in the collider, fraction is 0.0f;
                        {
                            minDistance = math.min(minDistance, hit[j].Fraction);
                        }
                    }
                }
                allRayData[i * 8 + 1] = minDistance;
            }

            // Collide with block level 0
             collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 1,
                GroupIndex = 0
            };
            PrepareRayCastInputs(ref Rinputs, ref pos, ref rot, ref collisionFilter, 0f);
            for (int i = 0; i < NumOfRayCasts; i++)
            {
                hit.Clear();
                bool haveHit = collisionWorld.CastRay(Rinputs[i], ref hit);
                float minDistance = 1.0f;

                if (haveHit)
                {

                    for (int j = 0; j < hit.Length; j++)
                    {
                        if (hit[j].Fraction > 0.001f) // Seems when ray cast starts in the collider, fraction is 0.0f;
                        {
                            minDistance = math.min(minDistance, hit[j].Fraction);
                        }
                    }
                }
                allRayData[i * 8+2] = minDistance;
            }
            // Collide with block level 1
             collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 1,
                GroupIndex = 0
            };
            PrepareRayCastInputs(ref Rinputs, ref pos, ref rot, ref collisionFilter, 1f);
            for (int i = 0; i < NumOfRayCasts; i++)
            {
                hit.Clear();
                bool haveHit = collisionWorld.CastRay(Rinputs[i], ref hit);
                float minDistance = 1.0f;

                if (haveHit)
                {

                    for (int j = 0; j < hit.Length; j++)
                    {
                        if (hit[j].Fraction > 0.001f) // Seems when ray cast starts in the collider, fraction is 0.0f;
                        {
                            minDistance = math.min(minDistance, hit[j].Fraction);
                        }
                    }
                }
                allRayData[i * 8 + 3] = minDistance;
            }
            // Collide with goal level 0
             collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 2,
                GroupIndex = 0
            };
            PrepareRayCastInputs(ref Rinputs, ref pos, ref rot, ref collisionFilter, 0f);
            for (int i = 0; i < NumOfRayCasts; i++)
            {
                hit.Clear();
                bool haveHit = collisionWorld.CastRay(Rinputs[i], ref hit);
                float minDistance = 1.0f;

                if (haveHit)
                {

                    for (int j = 0; j < hit.Length; j++)
                    {
                        if (hit[j].Fraction > 0.001f) // Seems when ray cast starts in the collider, fraction is 0.0f;
                        {
                            minDistance = math.min(minDistance, hit[j].Fraction);
                        }
                    }
                }
                allRayData[i * 8+4] = minDistance;
            }
            // Collide with goal level 1
             collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << 2,
                GroupIndex = 0
            };
            PrepareRayCastInputs(ref Rinputs, ref pos, ref rot, ref collisionFilter, 1f);
            for (int i = 0; i < NumOfRayCasts; i++)
            {
                hit.Clear();
                bool haveHit = collisionWorld.CastRay(Rinputs[i], ref hit);
                float minDistance = 1.0f;

                if (haveHit)
                {

                    for (int j = 0; j < hit.Length; j++)
                    {
                        if (hit[j].Fraction > 0.001f) // Seems when ray cast starts in the collider, fraction is 0.0f;
                        {
                            minDistance = math.min(minDistance, hit[j].Fraction);
                        }
                    }
                }
                allRayData[i * 8 + 5] = minDistance;
            }






            // If uninitialized, set the reset position
            if (cube.status == PushBlockStatus.UnInitialized)
            {
                cube.resetPosition = pos.Value;
                cube.status = PushBlockStatus.Ongoing;
            }

            if (cube.status == PushBlockStatus.Ongoing && cube.stepCount < 200){
                policy.RequestDecision(entity).SetObservationFromSlice(0, allRayData.Slice(0,72)).SetReward(-0.005f);
            }
            else if (cube.status == PushBlockStatus.Success){
                policy.EndEpisode(entity).SetObservationFromSlice(0, allRayData.Slice(0,72)).SetReward(1f);
                cube.status = PushBlockStatus.Ongoing;
                pos.Value = cube.resetPosition;
                positionData[entity] = pos;
                cube.stepCount = 0;
                rot.Value = quaternion.AxisAngle(new float3(0,1,0), 3.14f);
                var blockPosition = new Translation();
                blockPosition.Value = cube.resetPosition + new float3(6,0,-3);
                positionData[cube.block] = blockPosition;
            }
            else if (cube.status == PushBlockStatus.Ongoing && cube.stepCount >= 200)
            {
                policy.InterruptEpisode(entity).SetObservationFromSlice(0, allRayData.Slice(0,72));
                cube.status = PushBlockStatus.Ongoing;
                pos.Value = cube.resetPosition;
                positionData[entity] = pos;
                cube.stepCount = 0;
                rot.Value = quaternion.AxisAngle(new float3(0,1,0), 3.14f);
                var blockPosition = new Translation();
                blockPosition.Value = cube.resetPosition + new float3(6,0,-3);
                positionData[cube.block] = blockPosition;
            }
            cube.stepCount += 1;
            allRayData.Dispose();
        }).Schedule(inputDeps);

        var updateActionJob = new UpdatePushBlockAction
        {
            ComponentDataFromEntity = GetComponentDataFromEntity<PushBlockAction>(isReadOnly: false)
        };
        inputDeps = updateActionJob.Schedule(policy, inputDeps);

        inputDeps = Entities.WithAll<PushBlockCube>().ForEach((ref Translation pos, ref PhysicsVelocity vel, ref PushBlockAction action, ref Rotation rot) => {

            vel.Angular = 3 * new float3(0, action.Rotate - 1,0); // action (0,1,2) -> ( -1, 0, 1)
            float3 forward = math.forward(rot.Value);
            vel.Linear = 3 * forward * (action.Forward - 1);
        }).Schedule(inputDeps);
        inputDeps.Complete();
        return inputDeps;
    }



    static void PrepareRayCastInputs(ref NativeArray<Unity.Physics.RaycastInput> inputs, ref Translation pos, ref Rotation rot, ref CollisionFilter collisionFilter, float verticalOffset)
    {
        float3 forward = math.forward(rot.Value);
        float3 right = math.mul(rot.Value, new float3(1,0,0));//math.right(rot.Value);
        float3 up = math.mul(rot.Value, new float3(0,1,0));//math.right(rot.Value);math.up(rot.Value);
        float3 upOffset = verticalOffset * new float3(0,1,0);
        float RaycastCarRadius = 0f; //1.0f;
        float ObstacleSightDistance = 20f;

        int numRays = inputs.Length;


        for(int i = 0; i< numRays; i++){
            var angle = ((180f / (numRays - 1)) * i - 90f) * math.PI / 180f;
            var vec = forward * math.cos(angle) + right * math.sin(angle);
            inputs[i] = new RaycastInput{
                Filter = collisionFilter,
                Start = pos.Value + upOffset + vec * RaycastCarRadius,
                End = pos.Value + upOffset + vec * ObstacleSightDistance
            };
        }
    }


}
