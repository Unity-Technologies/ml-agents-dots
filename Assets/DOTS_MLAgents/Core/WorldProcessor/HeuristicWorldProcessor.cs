using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace DOTS_MLAgents.Core
{

    public static class HeristicWorldProcessorRegistringExtension
    {
        public static void SubscribeWorldWithHeuristic<TH>(
            this MLAgentsWorldSystem system,
            string policyId,
            MLAgentsWorld world,
            Func<TH> heuristic
            ) where TH : struct
        {
            var worldProcessor = new HeuristicWorldProcessor<TH>(world, heuristic);
            system.SubscribeWorld(policyId, world, worldProcessor, true);
        }

        public static void SubscribeWorldWithHeuristicForceNoCommunication<TH>(
            this MLAgentsWorldSystem system,
            string policyId,
            MLAgentsWorld world,
            Func<TH> heuristic
            ) where TH : struct
        {
            var worldProcessor = new HeuristicWorldProcessor<TH>(world, heuristic);
            system.SubscribeWorld(policyId, world, worldProcessor, false);
        }
    }

    public class HeuristicWorldProcessor<T> : IWorldProcessor where T : struct
    {

        private Func<T> heuristic;
        private MLAgentsWorld world;
        public HeuristicWorldProcessor(MLAgentsWorld world, Func<T> heuristic)
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
            for (int i = 0; i < world.AgentCounter.Count; i++)
            {
                if (world.ActionType == ActionType.CONTINUOUS)
                {

                    var s = world.ContinuousActuators.Slice(0, world.AgentCounter.Count * world.ActionSize).SliceConvert<T>();
                    s[i] = action;
                }
                else
                {
                    var s = world.DiscreteActuators.Slice(0, world.AgentCounter.Count * world.ActionSize).SliceConvert<T>();
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
