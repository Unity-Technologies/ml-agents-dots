using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using Unity.AI.MLAgents;

[DisableAutoCreation]
public class SimpleSystem : JobComponentSystem
{
    private MLAgentsWorld world;
    private NativeArray<Entity> entities;
    private Camera camera;

    public const int N_Agents = 5;
    int counter;

    // Start is called before the first frame update
    protected override void OnCreate()
    {
        Application.targetFrameRate = -1;

        world = new MLAgentsWorld(N_Agents, new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) }, ActionType.DISCRETE, 2, new int[] { 2, 3 });
        world.RegisterWorldWithHeuristic("test", () => new int2(1, 1));
        entities = new NativeArray<Entity>(N_Agents, Allocator.Persistent);

        for (int i = 0; i < N_Agents; i++)
        {
            entities[i] = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
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
        inputDeps.Complete();
        var reactiveJob = new UserCreatedActionEventJob
        {
            myNumber = 666
        };
        inputDeps = reactiveJob.Schedule(world, inputDeps);
        if (counter % 5 == 0)
        {
            var visObs = VisualObservationUtility.GetVisObs(camera, 84, 84, Allocator.TempJob);
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

        return inputDeps;
    }

    public struct UserCreateSensingJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> cameraObservation;
        public NativeArray<Entity> entities;
        public MLAgentsWorld world;

        public void Execute(int i)
        {
            world.RequestDecision(entities[i])
                .SetReward(1.0f)
                .SetObservation(0, new float3(entities[i].Index, 0, 0))
                .SetObservationFromSlice(1, cameraObservation.Slice());
        }
    }

    public struct UserCreatedActionEventJob : IActuatorJob
    {
        public int myNumber;
        public void Execute(ActuatorEvent data)
        {
            var tmp =data.GetAction<testAction>();
            Debug.Log(data.Entity.Index + "  " + tmp.e1);
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
