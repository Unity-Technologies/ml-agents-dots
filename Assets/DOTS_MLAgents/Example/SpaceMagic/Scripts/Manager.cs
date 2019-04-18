using DOTS_MLAgents.Core;
using DOTS_MLAgents.Core.Inference;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Runtime.InteropServices;

namespace DOTS_MLAgents.Example.SpaceMagic.Scripts
{
    public class SpaceSystemA : AgentSystem<Translation, Acceleration>{ }
    public class SpaceSystemB : AgentSystem<Translation, Acceleration>{ }
    public class SpaceSystemC : AgentSystem<Translation, Acceleration>{ }

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

        private SpaceSystemA sA;
        private SpaceSystemB sB;
        private SpaceSystemC sC;

        /// <summary>
        /// The sphere prefab
        /// </summary>
        public GameObject prefab;

        private Entity _prefabEntity;

        /// <summary>
        /// The Neural Network models for the three IAgentDecision
        /// </summary>
        public NNModel modelA;
        public NNModel modelB;
        public NNModel modelC;

        System.Func<Translation, Acceleration> heuristic;

        void Start()
        {
            manager = World.Active.EntityManager;


            sA=  World.Active.GetExistingSystem<SpaceSystemA>();
            sB=  World.Active.GetExistingSystem<SpaceSystemB>();
            sC=  World.Active.GetExistingSystem<SpaceSystemC>();

//            sA.Enabled = false;
//            sB.Enabled = false;
//            sC.Enabled = false;

            sA.SetNewComponentGroup(typeof(SphereGroup));
            sB.SetNewComponentGroup(typeof(SphereGroup));
            sC.SetNewComponentGroup(typeof(SphereGroup));

            sA.SetFilter<SphereGroup>(new SphereGroup{Group = 0});
            sB.SetFilter<SphereGroup>(new SphereGroup{Group = 1});
            sC.SetFilter<SphereGroup>(new SphereGroup{Group = 2});

            sA.Decision = new NNDecision<Translation, Acceleration>(modelA);
            sB.Decision = new NNDecision<Translation, Acceleration>(modelB);
            sC.Decision = new NNDecision<Translation, Acceleration>(modelC);

            heuristic = pos => {
                    float3 val = pos.Value;
                    var d = (val.x * val.x+ val.y * val.y + val.z * val.z);
                    val = - 3f * val;
                    return new Acceleration{
                        Value = new float3(-100f*val.z/d +val.x, 0 + val.y, 50f*val.x/d + val.z)
                    };
                };
            _prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);

            Spawn(1);
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

            if (Input.GetKeyDown((KeyCode.U)))
            {
                // sA.Decision = new HeuristicSpace(new float3(20,0,0), 3);
                sA.Decision = new HeuristicDecision<Translation, Acceleration>( heuristic);
            }
            if (Input.GetKeyDown((KeyCode.I)))
            {
                sA.Decision = new NNDecision<Translation, Acceleration>(modelA);
            }
            if (Input.GetKeyDown((KeyCode.J)))
            {
                sB.Decision = new HeuristicDecision<Translation, Acceleration>( heuristic);
            }
            if (Input.GetKeyDown((KeyCode.K)))
            {
                sB.Decision = new NNDecision<Translation, Acceleration>(modelB);
            }
            if (Input.GetKeyDown((KeyCode.N)))
            {
                sC.Decision = new HeuristicDecision<Translation, Acceleration>( heuristic);
            }
            if (Input.GetKeyDown((KeyCode.M)))
            {
                sC.Decision = new NNDecision<Translation, Acceleration>(modelC);
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
                manager.AddSharedComponentData(entities[i], new SphereGroup{Group = i%3});
                manager.SetComponentData(entities[i], new Acceleration());
                manager.SetComponentData(entities[i],
                    new Translation
                    {
                        Value = maxDistance * math.normalize(new float3(valX, valY, valZ))
                    });
                manager.SetComponentData(entities[i],
                    new Speed
                    {
                        Value = 10 * math.normalize(new float3(speedX, speedY, speedZ))
                    });
            }

            entities.Dispose();

        }

    }
}
