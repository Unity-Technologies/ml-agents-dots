using ECS_MLAgents_v0.Core;
using ECS_MLAgents_v0.Core.Inference;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ECS_MLAgents_v0.Example.SpaceWars.Scripts
{
    
    public class ShipSystemA : AgentSystem<ShipSensor, Steering> {}
//    public class ShipSystemB : AgentSystem<Position, Steering> {}


    public class Manager : MonoBehaviour
    {
        public float TargetAngle;
        public GameObject target;

        private EntityManager manager;
        public GameObject prefab;
        
//        public GameObject targetPrefab;

        private IAgentSystem _shipSystemA;

        private SensorPopulate _sensorSystem;
//        private IAgentSystem _shipSystemB;

        public NNModel model;

        void Start()
        {
            manager = World.Active.GetOrCreateManager<EntityManager>();

            _sensorSystem = World.Active.GetOrCreateManager<SensorPopulate>();

            _shipSystemA = World.Active.GetExistingManager<ShipSystemA>();
//            _shipSystemB = World.Active.GetExistingManager<ShipSystemB>();
            _shipSystemA.Decision = new WarsHeuristic();
//            _shipSystemA.Decision = new HumanDecision();
//            _shipSystemA.Decision = new NNDecision(model);
            _shipSystemA.DecisionInterval = 10;
//            _shipSystemB.DecisionInterval = 100;
//            _shipSystem.Decision = new ECS_MLAgents_v0.Example.SpaceMagic.Scripts.HeuristicSpace(new float3(), 100);
//            Spawn(1);
        }


        void Update()
        {
            float3 targetPos = 100 * new float3(math.cos(TargetAngle), 0, math.sin(TargetAngle));
            _sensorSystem.Center = targetPos;
            target.transform.position = targetPos;

            TargetAngle += Time.deltaTime/ 20f;
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
            manager.Instantiate(prefab, entities);
            for (int i = 0; i < amount; i++)
            {

                float valX = Random.Range(-1f, 1f);
                float valY = Random.Range(-1f, 1f);
                float valZ = Random.Range(-1f, 1f);
                float valD = Random.Range(0f, 1f);
                
                

                float3 SpawnOffset = valD *
                                     Globals.SPAWN_DISTANCE *
                                     math.normalize(new float3(valX, valY, valZ));

                manager.SetComponentData(entities[i], new Steering());
                manager.SetComponentData(entities[i],
                    new Position
                    {
                        Value = SpawnOffset 
                    });
                manager.SetComponentData(entities[i],
                    new Rotation
                    {
                        Value = quaternion.EulerXYZ(
//                        math.normalize(new float3(valX, valY, valZ))
                            new float3(0, 0, 1)
                        )
                    });
                manager.SetComponentData(entities[i],
                    new Scale
                    {
                        Value = new float3(
                            Globals.SHIP_SCALE_X,
                            Globals.SHIP_SCALE_Y,
                            Globals.SHIP_SCALE_Z)
                    });
//                 manager.AddSharedComponentData(entities[i], new DecisionPeriod{Phase = i % 5});
                manager.SetComponentData(entities[i], new Ship
                {
                    Fire = 0,
                    ReloadTime = Random.Range(0f, Globals.RELOAD_TIME),
                    TargetOffset = -SpawnOffset
                });
                manager.SetComponentData(entities[i], new ShipSensor());
            }

            entities.Dispose();





//            entities = new NativeArray<Entity>(amount, Allocator.Temp);
//            manager.Instantiate(targetPrefab, entities);
//            for (int i = 0; i < amount; i++)
//            {
//
//                float3 offset = new float3(0, 0, 0);
//                float valX = Random.Range(-1f, 1f);
//                float valY = Random.Range(-1f, 1f);
//                float valZ = Random.Range(-1f, 1f);
//                float valD = Random.Range(0f, 1f);
//
//                manager.SetComponentData(entities[i],
//                    new Position
//                    {
//                        Value = offset + valD *
//                                Globals.SPAWN_DISTANCE *
//                                math.normalize(new float3(valX, valY, valZ))
//                    });
//                manager.SetComponentData(entities[i],
//                    new Rotation
//                    {
//                        Value = quaternion.EulerXYZ(
////                        math.normalize(new float3(valX, valY, valZ))
//                            new float3(0, 0, 1)
//                        )
//                    });
//                manager.SetComponentData(entities[i],
//                    new Scale
//                    {
//                        Value = new float3(
//                            Globals.SHIP_SCALE_X,
//                            Globals.SHIP_SCALE_Y,
//                            Globals.SHIP_SCALE_Z)
//                    });
////                 manager.AddSharedComponentData(entities[i], new DecisionPeriod{Phase = i % 5});
//                manager.SetComponentData(entities[i], new TargetShip
//                {
//                    RotationAxis = new float3(
//                        Random.Range(-1f, 1f),
//                        Random.Range(-1f, 1f),
//                        Random.Range(-1f, 1f))
//                });
//            }
//
//            entities.Dispose();
        }
    }

}

