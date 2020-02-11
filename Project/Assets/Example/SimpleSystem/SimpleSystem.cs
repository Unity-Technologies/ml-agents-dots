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
using Unity.AI.MLAgents;


// [UpdateInGroup(typeof(SimulationSystemGroup))]
// [UpdateAfter(typeof(MLAgentsSystem))]
[DisableAutoCreation]
public class SimpleSystem : JobComponentSystem
{
    private MLAgentsSystem sys;
    private MLAgentsWorld world;
    private NativeArray<Entity> entities;
    private Camera camera;

    public const int N_Agents = 5;
    int counter;

    // Start is called before the first frame update
    protected override void OnCreate()
    {
        Application.targetFrameRate = -1;
        sys = World.Active.GetOrCreateSystem<MLAgentsSystem>();

        world = new MLAgentsWorld(N_Agents, ActionType.DISCRETE, new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) }, 2, new int[] { 2, 3 });
        sys.SubscribeWorldWithHeuristic("test", world, () => new int2(1, 1));
        entities = new NativeArray<Entity>(N_Agents, Allocator.Persistent);
        // World.Active.EntityManager.CreateEntity(entities);
        for (int i = 0; i < N_Agents; i++)
        {
            entities[i] = World.Active.EntityManager.CreateEntity();
        }
    }

    protected override void OnDestroy()
    {
        world.Dispose();
        entities.Dispose();
    }

    // Update is called once per frame
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (camera == null)
        {
            camera = Camera.main;
            camera = GameObject.FindObjectOfType<Camera>();
        }
        // inputDeps.Complete();
        var reactiveJob = new UserCreatedActionEventJob
        {
            myNumber = 666
        };
        inputDeps = reactiveJob.Schedule(world, inputDeps);

        if (counter % 5 == 0)
        {
            var visObs = VisualObservationUtility.GetVisObs(camera, 84, 84);
            var senseJob = new UserCreateSensingJob
            {
                cameraObservation = visObs,
                entities = entities,
                world = world
            };
            inputDeps = senseJob.Schedule(N_Agents, 64, inputDeps);

            inputDeps.Complete();
            visObs.Dispose();
        }
        counter++;
        sys.RegisterDependency(inputDeps);

        // inputDeps = sys.ManualUpdate(inputDeps);

        // inputDeps.Complete();

        return inputDeps;
    }

    // [BurstCompile]
    public struct UserCreateSensingJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> cameraObservation;
        public NativeArray<Entity> entities;
        public MLAgentsWorld world;

        public void Execute(int i)
        {
            // world.CollectData(entities[i], new float3(entities[i].Index, 0, 0));
            world.RequestDecision(entities[i])
                .SetReward(1.0f)
                .SetObservation(0, new float3(entities[i].Index, 0, 0))
                .SetObservationFromSlice(1, cameraObservation.Slice());
        }
    }

    // [BurstCompile]
    public struct UserCreatedActionEventJob : IActuatorJob
    {
        public int myNumber;
        public void Execute(ActuatorEvent data)
        {
            var tmp = new testAction();
            data.GetDiscreteAction(out tmp);
            // Debug.Log(data.Entity.Index + "  " + tmp.x);
            // Debug.Log(data.Entity.Index + "  " + tmp.e1);
        }
    }

    public enum testEnum
    {
        A, B, C
    }
    public struct testAction
    {
        public testEnum e1;
        public testEnum e2;
    }
}
