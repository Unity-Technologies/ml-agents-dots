using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class BalanceBallManager : MonoBehaviour
{
    private EntityManager manager;
    public GameObject prefabPlatform;
    public GameObject prefabBall;
    private Entity _prefabEntityPlatform;
    private Entity _prefabEntityBall;
    int currentIndex;
    // Start is called before the first frame update
    void Start()
    {
        manager = World.Active.EntityManager;
        _prefabEntityPlatform = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabPlatform, World.Active);
        _prefabEntityBall = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefabBall, World.Active);
        Time.captureFramerate = 60;
        Spawn(100);
    }

    void Spawn(int amount)
    {
        NativeArray<Entity> entitiesP = new NativeArray<Entity>(amount, Allocator.Temp);
        NativeArray<Entity> entitiesB = new NativeArray<Entity>(amount, Allocator.Temp);
        manager.Instantiate(_prefabEntityPlatform, entitiesP);
        manager.Instantiate(_prefabEntityBall, entitiesB);
        for (int i = 0; i < amount; i++)
        {
            float valX = Random.Range(-0.1f, 0.1f);
            float valZ = Random.Range(-0.1f, 0.1f);
            manager.SetComponentData(entitiesP[i],
                new Translation
                {
                    Value = new float3(currentIndex * 2f, 0, 0)
                });
            manager.SetComponentData(entitiesB[i],
                new Translation
                {
                    Value = new float3(currentIndex * 2f, 0.2f, 0)
                });

            manager.SetComponentData(entitiesP[i],
                new Rotation
                {
                    Value = quaternion.EulerXYZ(valX, 0, valZ)
                });
            manager.AddComponent<RefToPlatform>(entitiesB[i]);
            manager.SetComponentData(entitiesB[i], new RefToPlatform { Value = entitiesP[i] });
            manager.AddComponent<BallResetData>(entitiesB[i]);
            manager.SetComponentData(entitiesB[i], new BallResetData { ResetPosition = new float3(currentIndex * 2f, 0.2f, 0) });
            manager.AddComponent<BallData>(entitiesP[i]);
            manager.AddComponent<AngularAcceleration>(entitiesP[i]);
            currentIndex++;
        }

        entitiesP.Dispose();
        entitiesB.Dispose();

    }

}
