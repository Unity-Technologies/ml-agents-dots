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
    [UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
    public class ZKSystem : AgentSystem<Sensor, Speed>{ }
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

        private ZKSystem sA;

        
        /// <summary>
        /// The sphere prefab
        /// </summary>
        public GameObject prefab;

        [Range(0,100)]
        public float TimeScale=1;
        
        
        void Start()
        {

            Screen.SetResolution(1280, 720, false);
            QualitySettings.SetQualityLevel(0, true);
            Time.timeScale = 100;
            Time.captureFramerate = 60;
            Application.targetFrameRate = -1;


            Time.captureFramerate = 60;
            manager = World.Active.EntityManager;
            

            sA=  World.Active.GetExistingSystem<ZKSystem>(); 
//            sB=  World.Active.GetExistingManager<SpaceSystemB>();
//            sC=  World.Active.GetExistingManager<SpaceSystemC>();
            
//            sA.Enabled = false;
//            sB.Enabled = false;
//            sC.Enabled = false;

//            sA.SetNewComponentGroup(typeof(SphereGroup));
//            sB.SetNewComponentGroup(typeof(SphereGroup));
//            sC.SetNewComponentGroup(typeof(SphereGroup));
//            
//            sA.SetFilter<SphereGroup>(new SphereGroup{Group = 0});
//            sB.SetFilter<SphereGroup>(new SphereGroup{Group = 1});
//            sC.SetFilter<SphereGroup>(new SphereGroup{Group = 2});
                sA.Decision = new HeuristicDecision<Sensor, Speed>(s => new Speed{Value = new float3(1,1,1)});
            // sA.Decision = new ExternalDecision<Sensor, Speed>();
        //    sA.Decision = new SpaceMagic.Scripts.HeuristicSpace(new float3(0,0,0), 1f);

        //    sA.Decision = new NNDecision(modelA);
//            sB.Decision = new NNDecision(modelB);
//            sC.Decision = new NNDecision(modelC);
             sA.DecisionRequester = new FixedCountRequester(1);

            //  Time.captureFramerate = 60;
             
            Spawn(100);
        }

        void Update()
        {
            Time.timeScale = TimeScale;
        }



        void Spawn(int amount)
        {
            NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);
            manager.Instantiate(prefab, entities);
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
