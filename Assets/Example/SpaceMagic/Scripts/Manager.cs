using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DOTS_MLAgents.Example.SpaceMagic.Scripts
{
    public class Manager : MonoBehaviour
    {
        /// <summary>
        /// The distance at which the spheres are instantiated from the center
        /// </summary>
        public float maxDistance;

        private EntityManager manager;


        /// <summary>
        /// The sphere prefab
        /// </summary>
        public GameObject prefab;

        private Entity _prefabEntity;


        void Start()
        {
            World.Active.GetOrCreateSystem<SpaceMagicMovementSystem>();
            manager = World.Active.EntityManager;
            // sys.SetModel("SpaceMagic", modelA);
            _prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);

            Spawn(100);
        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Spawn(1);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Spawn(100);
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                Spawn(1000);
            }

        }

        void Spawn(int amount)
        {
            NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);
            manager.Instantiate(_prefabEntity, entities);
            for (int i = 0; i < amount; i++)
            {
                float valX = Random.Range(-1f, 1f);
                float valY = Random.Range(-1f, 1f);
                float valZ = Random.Range(-1f, 1f);

                float speedX = Random.Range(-1f, 1f);
                float speedY = Random.Range(-1f, 1f);
                float speedZ = Random.Range(-1f, 1f);
                manager.AddSharedComponentData(entities[i], new SphereGroup { Group = i % 3 });
                manager.AddComponentData(entities[i], new Acceleration());
                manager.SetComponentData(entities[i],
                    new Translation
                    {
                        Value = maxDistance * math.normalize(new float3(valX, valY, valZ))
                    });
                manager.AddComponentData(entities[i],
                    new Speed
                    {
                        Value = 10 * math.normalize(new float3(speedX, speedY, speedZ))
                    });
            }

            entities.Dispose();

        }

    }
}
