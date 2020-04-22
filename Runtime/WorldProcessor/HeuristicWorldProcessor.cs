using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    public static class HeristicWorldProcessorRegistringExtension
    {
        /// <summary>
        /// Registers the given MLAgentsWorld to the Academy with a Heuristic.
        /// Note that if the simulation connects to Python, the Heuristic will
        /// be ignored and the world will exchange data with Python instead.
        /// The Heuristic is a Function that returns an action struct.
        /// </summary>
        /// <param name="world"> The MLAgentsWorld to register</param>
        /// <param name="policyId"> The name of the world. This is useful for identification
        /// and for training.</param>
        /// <param name="heuristic"> The Heuristic used to generate the actions.
        /// Note that all agents in the world will receive the same action.</param>
        /// <typeparam name="TH"> The type of the Action struct. It must match the Action
        /// Size and Action Type of the world.</typeparam>
        public static void RegisterWorldWithHeuristic<TH>(
            this MLAgentsWorld world,
            string policyId,
            Func<TH> heuristic
        ) where TH : struct
        {
            var worldProcessor = new HeuristicWorldProcessor<TH>(world, heuristic);
            Academy.Instance.RegisterWorld(policyId, world, worldProcessor, true);
        }

        /// <summary>
        /// Registers the given MLAgentsWorld to the Academy with a Heuristic.
        /// Note that if the simulation connects to Python, the world will not
        /// exchange data with Python and use the Heuristic regardless.
        /// The Heuristic is a Function that returns an action struct.
        /// </summary>
        /// <param name="world"> The MLAgentsWorld to register</param>
        /// <param name="policyId"> The name of the world. This is useful for identification
        /// and for training.</param>
        /// <param name="heuristic"> The Heuristic used to generate the actions.
        /// Note that all agents in the world will receive the same action.</param>
        /// <typeparam name="TH"> The type of the Action struct. It must match the Action
        /// Size and Action Type of the world.</typeparam>
        public static void RegisterWorldWithHeuristicForceNoCommunication<TH>(
            this MLAgentsWorld world,
            string policyId,
            Func<TH> heuristic
        ) where TH : struct
        {
            var worldProcessor = new HeuristicWorldProcessor<TH>(world, heuristic);
            Academy.Instance.RegisterWorld(policyId, world, worldProcessor, false);
        }
    }

    internal class HeuristicWorldProcessor<T> : IWorldProcessor where T : struct
    {
        private Func<T> heuristic;
        private MLAgentsWorld world;

        public bool IsConnected {get {return false;}}

        internal HeuristicWorldProcessor(MLAgentsWorld world, Func<T> heuristic)
        {
            this.world = world;
            this.heuristic = heuristic;
            var structSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
            if (structSize != world.ActionSize)
            {
                throw new MLAgentsException(
                    $"The heuristic provided does not match the action size. Expected {world.ActionSize} but received {structSize} from heuristic");
            }
        }

        public void ProcessWorld()
        {
            T action = heuristic.Invoke();
            var totalCount = world.DecisionCounter.Count;

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
        }

        public void Dispose()
        {
        }
    }
}
