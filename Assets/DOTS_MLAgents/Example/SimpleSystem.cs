using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using DOTS_MLAgents.Core;



// [UpdateInGroup(typeof(SimulationSystemGroup))]
// [UpdateAfter(typeof(MLAgentsWorldSystem))]
[DisableAutoCreation]
public class SimpleSystem : JobComponentSystem
{
    private MLAgentsWorldSystem sys;
    private MLAgentsWorld world;
    private NativeArray<Entity> entities;

    public const int N_Agents = 5;

    // Start is called before the first frame update
    protected override void OnCreate()
    {
        Application.targetFrameRate = -1;
        sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();

        world = new MLAgentsWorld(100, ActionType.CONTINUOUS, new int3[] { new int3(3, 0, 0) }, 3);
        sys.SubscribeWorld("test", world);

        entities = new NativeArray<Entity>(N_Agents, Allocator.Persistent);
        // World.Active.EntityManager.CreateEntity(entities);
        for (int i = 0; i < N_Agents; i++)
        {
            entities[i] = World.Active.EntityManager.CreateEntity();
        }

    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // inputDeps.Complete();

        var reactiveJob = new UserCreatedActionEventJob
        {
            myNumber = 666
        };
        inputDeps = reactiveJob.Schedule(world, inputDeps);

        var senseJob = new UserCreateSensingJob
        {
            entities = entities,
            world = world
        };
        inputDeps = senseJob.Schedule(N_Agents, MLAgentsWorldSystem.n_threads, inputDeps);
        sys.RegisterDependency(inputDeps);

        // inputDeps = sys.ManualUpdate(inputDeps);

        // inputDeps.Complete();




        return inputDeps;
    }

    [BurstCompile]
    public struct UserCreateSensingJob : IJobParallelFor
    {
        public NativeArray<Entity> entities;
        public MLAgentsWorld world;

        public void Execute(int i)
        {
            // world.CollectData(entities[i], new float3(entities[i].Index, 0, 0));
            world.RequestDecision(entities[i])
                .SetReward(1.0f)
                .SetObservation(0, new float3(entities[i].Index, 0, 0));
            // Debug.Log("REQUESTING DECISION");

        }
    }

    // [BurstCompile]
    public struct UserCreatedActionEventJob : IActuatorJob
    {
        public int myNumber;
        public void Execute(ActuatorEvent data)
        {
            var tmp = new float3();
            data.GetContinuousAction(out tmp);
            Debug.Log(data.Entity.Index + "  " + tmp.x);
        }
    }

}
