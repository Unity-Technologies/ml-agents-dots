using DOTS_MLAgents.Core;
using DOTS_MLAgents.Core.Inference;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DOTS_MLAgents.Example.ZeroK.Scripts
{
    //    public class SpaceSystemB : AgentSystem<Position, Acceleration>{ }
    //    public class SpaceSystemC : AgentSystem<Position, Acceleration>{ }

    /// <summary>
    /// Manager is responsible for instantiation the spheres and the IAgentSystem that will make
    /// them move.
    ///
    /// There are three IAgentSystem, each updating a third of the spheres with a different NNModel.
    /// You can spawn new sphere :
    ///     A : 1 sphere
    ///     S : 100 spheres
    ///     D : 1000 spheres
    /// You can change the decision of each of the IAgentSystem
    ///
    ///     U : Give a Heuristic to the first IAgentSystem
    ///     I : Give a NNModel to the first IAgentSystem
    ///
    ///     J : Give a Heuristic to the second IAgentSystem
    ///     K : Give a NNModel to the second IAgentSystem
    ///
    ///     N : Give a Heuristic to the third IAgentSystem
    ///     M : Give a NNModel to the third IAgentSystem
    /// 
    /// </summary>
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

        [Range(0, 100)]
        public float TimeScale = 1;


        void Start()
        {
            Screen.SetResolution(1280, 720, false);
            QualitySettings.SetQualityLevel(0, true);
            Time.timeScale = 100;
            Time.captureFramerate = 60;
            Application.targetFrameRate = -1;


            Time.captureFramerate = 60;
            manager = World.Active.EntityManager;

            //  Time.captureFramerate = 60;
            _prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);

            Spawn(100);
        }

        void Update()
        {
            Time.timeScale = TimeScale;
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
                manager.AddComponentData(entities[i], new Sensor());
                manager.AddComponentData(entities[i],
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
