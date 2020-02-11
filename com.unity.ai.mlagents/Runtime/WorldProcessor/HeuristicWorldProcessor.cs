using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    public static class HeristicWorldProcessorRegistringExtension
    {
        public static void SubscribeWorldWithHeuristic<TH>(
            this MLAgentsSystem system,
            string policyId,
            MLAgentsWorld world,
            Func<TH> heuristic
        ) where TH : struct
        {
            var worldProcessor = new HeuristicWorldProcessor<TH>(world, heuristic);
            system.SubscribeWorld(policyId, world, worldProcessor, true);
        }

        public static void SubscribeWorldWithHeuristicForceNoCommunication<TH>(
            this MLAgentsSystem system,
            string policyId,
            MLAgentsWorld world,
            Func<TH> heuristic
        ) where TH : struct
        {
            var worldProcessor = new HeuristicWorldProcessor<TH>(world, heuristic);
            system.SubscribeWorld(policyId, world, worldProcessor, false);
        }
    }

    internal class HeuristicWorldProcessor<T> : IWorldProcessor where T : struct
    {
        private Func<T> heuristic;
        private MLAgentsWorld world;
        internal HeuristicWorldProcessor(MLAgentsWorld world, Func<T> heuristic)
        {
            this.world = world;
            this.heuristic = heuristic;
            var structSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
            if (structSize != world.ActionSize)
            {
                throw new MLAgentsException(string.Format(
                    "The heuristic provided does not match the action size. Expected {0} received {1}", structSize, world.ActionSize));
            }
        }

        public void ProcessWorld()
        {
            T action = heuristic.Invoke();
            var totalCount = world.AgentCounter.Count;

            // TODO : This can be parallelized
            if (world.ActionType == ActionType.CONTINUOUS)
            {
                var s = world.ContinuousActuators.Slice(0, totalCount * world.ActionSize).SliceConvert<T>();
                for (int i = 0; i < totalCount; i++)
                {
                    s[i] = action;
                }
            }
            else
            {
                var s = world.DiscreteActuators.Slice(0, totalCount * world.ActionSize).SliceConvert<T>();
                for (int i = 0; i < totalCount; i++)
                {
                    s[i] = action;
                }
            }
            world.SetActionReady();
            world.ResetDecisionsCounter();
        }

        public void ResetWorld()
        {
            world.ResetActionsCounter();
            world.ResetDecisionsCounter();
        }

        public void Dispose()
        {
        }
    }
}
