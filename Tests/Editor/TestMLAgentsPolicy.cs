using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestMLAgentsPolicy
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
        }

        [TearDown]
        public void TearDownBase()
        {
            ECSWorld.Dispose();
        }

        [Test]
        public void TestAcademyCreation()
        {
            var instance = Academy.Instance;
            Assert.True(Academy.IsInitialized);
            Academy.Instance.Dispose();
        }

        [Test]
        public void TestPolicyCreation()
        {
            var policy = new Policy(
                20,
                new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) },
                ActionType.DISCRETE,
                2,
                new int[] { 2, 3 });
            policy.Dispose();
        }

        private struct SingleActionEnumUpdate : IActuatorJob
        {
            public NativeArray<Entity> ent;
            public NativeArray<DiscreteAction_TWO_THREE> action;
            public void Execute(ActuatorEvent data)
            {
                ent[0] = data.Entity;
                var act = data.GetAction<DiscreteAction_TWO_THREE>();
                action[0] = act;
            }
        }

        [Test]
        public void TestManualDecisionSteppingWithHeuristic()
        {
            var policy = new Policy(
                20,
                new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) },
                ActionType.DISCRETE,
                2,
                new int[] { 2, 3 });

            policy.RegisterPolicyWithHeuristic("test", () => new int2(0, 1));

            var entity = entityManager.CreateEntity();

            policy.RequestDecision(entity)
                .SetObservation(0, new float3(1, 2, 3))
                .SetReward(1f);

            var entities = new NativeArray<Entity>(1, Allocator.Persistent);
            var actions = new NativeArray<DiscreteAction_TWO_THREE>(1, Allocator.Persistent);

            var actionJob = new SingleActionEnumUpdate
            {
                ent = entities,
                action = actions
            };
            actionJob.Schedule(policy, new JobHandle()).Complete();

            Assert.AreEqual(entity, entities[0]);
            Assert.AreEqual(new DiscreteAction_TWO_THREE
            {
                action_ONE = TwoOptionEnum.Option_ONE,
                action_TWO = ThreeOptionEnum.Option_TWO
            }, actions[0]);

            policy.Dispose();
            entities.Dispose();
            actions.Dispose();
            Academy.Instance.Dispose();
        }

        [Test]
        public void TestTerminateEpisode()
        {
            var policy = new Policy(
                20,
                new int3[] { new int3(3, 0, 0), new int3(84, 84, 3) },
                ActionType.DISCRETE,
                2,
                new int[] { 2, 3 });

            policy.RegisterPolicyWithHeuristic("test", () => new int2(0, 1));

            var entity = entityManager.CreateEntity();

            policy.RequestDecision(entity)
                .SetObservation(0, new float3(1, 2, 3))
                .SetReward(1f);

            var hashMap = policy.GenerateActionHashMap<DiscreteAction_TWO_THREE>(Allocator.Temp);
            Assert.True(hashMap.TryGetValue(entity, out _));
            hashMap.Dispose();

            policy.EndEpisode(entity);
            hashMap = policy.GenerateActionHashMap<DiscreteAction_TWO_THREE>(Allocator.Temp);
            Assert.False(hashMap.TryGetValue(entity, out _));
            hashMap.Dispose();

            policy.Dispose();
            Academy.Instance.Dispose();
        }

        [Test]
        public void TestMultiPolicy()
        {
            var policy1 = new Policy(
                20,
                new int3[] { new int3(3, 0, 0) },
                ActionType.DISCRETE,
                2,
                new int[] { 2, 3 });
            var policy2 = new Policy(
                20,
                new int3[] { new int3(3, 0, 0) },
                ActionType.DISCRETE,
                2,
                new int[] { 2, 3 });

            policy1.RegisterPolicyWithHeuristic("test1", () => new DiscreteAction_TWO_THREE
            {
                action_ONE = TwoOptionEnum.Option_TWO,
                action_TWO = ThreeOptionEnum.Option_ONE
            });
            policy2.RegisterPolicyWithHeuristic("test2", () => new DiscreteAction_TWO_THREE
            {
                action_ONE = TwoOptionEnum.Option_ONE,
                action_TWO = ThreeOptionEnum.Option_TWO
            });

            var entity = entityManager.CreateEntity();

            policy1.RequestDecision(entity);
            policy2.RequestDecision(entity);

            var entities = new NativeArray<Entity>(1, Allocator.Persistent);
            var actions = new NativeArray<DiscreteAction_TWO_THREE>(1, Allocator.Persistent);
            var actionJob1 = new SingleActionEnumUpdate
            {
                ent = entities,
                action = actions
            };
            actionJob1.Schedule(policy1, new JobHandle()).Complete();

            Assert.AreEqual(entity, entities[0]);
            Assert.AreEqual(new DiscreteAction_TWO_THREE
            {
                action_ONE = TwoOptionEnum.Option_TWO,
                action_TWO = ThreeOptionEnum.Option_ONE
            }, actions[0]);
            entities.Dispose();
            actions.Dispose();

            entities = new NativeArray<Entity>(1, Allocator.Persistent);
            actions = new NativeArray<DiscreteAction_TWO_THREE>(1, Allocator.Persistent);
            var actionJob2 = new SingleActionEnumUpdate
            {
                ent = entities,
                action = actions
            };
            actionJob2.Schedule(policy2, new JobHandle()).Complete();

            Assert.AreEqual(entity, entities[0]);
            Assert.AreEqual(new DiscreteAction_TWO_THREE
            {
                action_ONE = TwoOptionEnum.Option_ONE,
                action_TWO = ThreeOptionEnum.Option_TWO
            }, actions[0]);
            entities.Dispose();
            actions.Dispose();

            policy1.Dispose();
            policy2.Dispose();
            Academy.Instance.Dispose();
        }
    }
}
