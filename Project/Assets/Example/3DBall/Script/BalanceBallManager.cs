using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using Barracuda;
using Unity.AI.MLAgents;

public class BalanceBallManager : MonoBehaviour
{
    private EntityManager manager;
    public GameObject prefabPlatform;
    public GameObject prefabBall;
    private Entity _prefabEntityPlatform;
    private Entity _prefabEntityBall;
    int currentIndex;
    // Start is called before the first frame update

    public NNModel model;

    void Awake()
    {
        var ballSystem = World.Active.GetOrCreateSystem<BallSystem>();
        var world = new MLAgentsWorld(1000, ActionType.CONTINUOUS, new int3[] { new int3(4, 0, 0), new int3(3, 0, 0) }, 2);
        ballSystem.world = world;
        var mlsys = World.Active.GetOrCreateSystem<MLAgentsWorldSystem>();
        mlsys.SubscribeWorldWithBarracudaModel("3DBallDOTS", world, model);


        manager = World.Active.EntityManager;

        BlobAssetStore blob = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blob);
        _prefabEntityPlatform = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabPlatform, settings);
        _prefabEntityBall = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabBall, settings);

        Time.captureFramerate = 60;
        Spawn(1000);
        blob.Dispose();
    }

    void Spawn(int amount)
    {
        NativeArray<Entity> entitiesP = new NativeArray<Entity>(amount, Allocator.Temp);
        NativeArray<Entity> entitiesB = new NativeArray<Entity>(amount, Allocator.Temp);
        manager.Instantiate(_prefabEntityPlatform, entitiesP);
        manager.Instantiate(_prefabEntityBall, entitiesB);
        for (int i = 0; i < amount; i++)
        {
            float3 position = new float3((currentIndex % 10) - 5, (currentIndex / 10 % 10) - 5, currentIndex / 100) * 2f;
            float valX = Random.Range(-0.1f, 0.1f);
            float valZ = Random.Range(-0.1f, 0.1f);
            manager.SetComponentData(entitiesP[i],
                new Translation
                {
                    Value = position
                });
            manager.SetComponentData(entitiesB[i],
                new Translation
                {
                    Value = position + new float3(0, 0.2f, 0)
                });

            manager.SetComponentData(entitiesP[i],
                new Rotation
                {
                    Value = quaternion.EulerXYZ(valX, 0, valZ)
                });
            manager.AddComponent<RefToPlatform>(entitiesB[i]);
            manager.SetComponentData(entitiesB[i], new RefToPlatform { Value = entitiesP[i] });
            manager.AddComponent<BallResetData>(entitiesB[i]);
            manager.SetComponentData(entitiesB[i], new BallResetData { ResetPosition = position + new float3(0, 0.2f, 0) });
            manager.AddComponent<BallData>(entitiesP[i]);
            manager.AddComponent<AngularAcceleration>(entitiesP[i]);
            currentIndex++;
        }

        entitiesP.Dispose();
        entitiesB.Dispose();

    }

}
