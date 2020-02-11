using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestMLAgentsWorld
    {
        private World ECSWorld;
        private EntityManager entityManager;

        private enum TwoOptionEnum
        {
            Option_ONE,
            Option_TWO
        }

        private enum ThreeOptionEnum
        {
            Option_ONE,
            Option_TWO,
            Option_THREE
        }
        private struct DiscreteAction_TWO_THREE
        {
            public TwoOptionEnum action_ONE;
            public ThreeOptionEnum action_TWO;
        }

        [SetUp]
        public void SetUpBase()
        {
            ECSWorld = new World("Test World");
            entityManager = ECSWorld.EntityManager;
            // em = w.EntityManager;
            // emq = new EntityManagerUtility(w);
            // var allSystems =
            //     DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default, requireExecuteAlways: false);
            // allSystems.Add(typeof(ConstantDeltaTimeSystem)); //this has disable auto creation on it.
            // DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(w, allSystems);
        }

        [TearDown]
        public void TearDownBase()
        {
            ECSWorld.Dispose();
        }

        [Test]
        public void TestCreation()
        {
            var world = new MLAgentsWorld(
                20,
                ActionType.DISCRETE,
                new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) },
                2,
                new int[] { 2, 3 });
            world.Dispose();
        }

        private struct SingleActionEnumUpdate : IActuatorJob
        {
            public NativeArray<Entity> ent;
            public NativeArray<DiscreteAction_TWO_THREE> action;
            public void Execute(ActuatorEvent data)
            {
                ent[0] = data.Entity;
                DiscreteAction_TWO_THREE act = new DiscreteAction_TWO_THREE();
                data.GetDiscreteAction<DiscreteAction_TWO_THREE>(out act);
                action[0] = act;
            }
        }

        [Test]
        public void TestManualDecisionSteppingWithHeuristic()
        {
            var system = ECSWorld.GetOrCreateSystem<MLAgentsSystem>();
            var world = new MLAgentsWorld(
                20,
                ActionType.DISCRETE,
                new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) },
                2,
                new int[] { 2, 3 });

            system.SubscribeWorldWithHeuristic("test", world, () => new int2(0, 1));

            var entity = entityManager.CreateEntity();

            world.RequestDecision(entity);
            system.ManualUpdate(new JobHandle()).Complete();

            var entities = new NativeArray<Entity>(1, Allocator.Persistent);
            var actions = new NativeArray<DiscreteAction_TWO_THREE>(1, Allocator.Persistent);

            var actionJob = new SingleActionEnumUpdate
            {
                ent = entities,
                action = actions
            };
            actionJob.Schedule(world, new JobHandle()).Complete();

            Assert.AreEqual(entity, entities[0]);
            Assert.AreEqual(new DiscreteAction_TWO_THREE
            {
                action_ONE = TwoOptionEnum.Option_ONE,
                action_TWO = ThreeOptionEnum.Option_TWO
            }, actions[0]);

            world.Dispose();
            entities.Dispose();
            actions.Dispose();
        }
    }
}
