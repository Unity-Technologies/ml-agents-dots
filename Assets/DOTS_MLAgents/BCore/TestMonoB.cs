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

public class TestMonoB : JobComponentSystem
{
    private MLAgentsWorldSystem sys;
    private MLAgentsWorld world;
    private NativeArray<Entity> entities;
    private int counter;

    // Start is called before the first frame update
    protected override void OnCreate()
    {
        sys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
        world = sys.GetExistingMLAgentsWorld<float3, float3>("test");
        entities = new NativeArray<Entity>(5, Allocator.Persistent);
        for (int i = 0; i < 5; i++)
        {
            entities[i] = World.Active.EntityManager.CreateEntity();
        }

    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        counter++;

        var senseJob = new UserCreateSensingJob
        {
            entities = entities,
            world = world
        };

        inputDeps = senseJob.Schedule(5, 2, inputDeps);

        inputDeps = sys.ManualUpdate(inputDeps);

        inputDeps.Complete();

        var reactiveJob = new UserCreatedActionEventJob
        {
            myNumber = 666
        };
        inputDeps = reactiveJob.Schedule(world.ActuatorDataHolder, inputDeps);


        inputDeps.Complete();
        return inputDeps;
    }

    public struct UserCreateSensingJob : IJobParallelFor
    {
        public NativeArray<Entity> entities;
        public MLAgentsWorld world;

        public void Execute(int i)
        {
            world.DataCollector.CollectData(entities[i], new float3(entities[i].Index, 0, 0));
        }
    }

    public struct UserCreatedActionEventJob : IActuatorJob
    {
        public int myNumber;
        public void Execute(ActuatorEvent data)
        {
            Debug.Log(data.Entity.Index + "  " + data.GetAction<float3>().x);
        }
    }

}
