using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    public static class HeristicPolicyProcessorRegistringExtension
    {
        /// <summary>
        /// Registers the given Policy to the Academy with a Heuristic.
        /// Note that if the simulation connects to Python, the Heuristic will
        /// be ignored and the Policy will exchange data with Python instead.
        /// The Heuristic is a Function that returns an action struct.
        /// </summary>
        /// <param name="policy"> The Policy to register</param>
        /// <param name="policyId"> The name of the Policy. This is useful for identification
        /// and for training.</param>
        /// <param name="heuristic"> The Heuristic used to generate the actions.
        /// Note that all agents in the Policy will receive the same action.</param>
        /// <typeparam name="TH"> The type of the Action struct. It must match the Action
        /// Size and Action Type of the Policy.</typeparam>
        public static void RegisterPolicyWithHeuristic<TH>(
            this Policy policy,
            string policyId,
            Func<TH> heuristic
        ) where TH : struct
        {
            var policyProcessor = new HeuristicPolicyProcessor<TH>(policy, heuristic);
            Academy.Instance.RegisterPolicy(policyId, policy, policyProcessor, true);
        }

        /// <summary>
        /// Registers the given Policy to the Academy with a Heuristic.
        /// Note that if the simulation connects to Python, the Policy will not
        /// exchange data with Python and use the Heuristic regardless.
        /// The Heuristic is a Function that returns an action struct.
        /// </summary>
        /// <param name="policy"> The Policy to register</param>
        /// <param name="policyId"> The name of the Policy. This is useful for identification
        /// and for training.</param>
        /// <param name="heuristic"> The Heuristic used to generate the actions.
        /// Note that all agents in the Policy will receive the same action.</param>
        /// <typeparam name="TH"> The type of the Action struct. It must match the Action
        /// Size and Action Type of the Policy.</typeparam>
        public static void RegisterPolicyWithHeuristicForceNoCommunication<TH>(
            this Policy policy,
            string policyId,
            Func<TH> heuristic
        ) where TH : struct
        {
            var policyProcessor = new HeuristicPolicyProcessor<TH>(policy, heuristic);
            Academy.Instance.RegisterPolicy(policyId, policy, policyProcessor, false);
        }
    }

    internal class HeuristicPolicyProcessor<T> : IPolicyProcessor where T : struct
    {
        private Func<T> m_Heuristic;
        private Policy m_Policy;

        public bool IsConnected {get {return false;}}

        internal HeuristicPolicyProcessor(Policy policy, Func<T> heuristic)
        {
            this.m_Policy = policy;
            this.m_Heuristic = heuristic;
            var structSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
            if (structSize != policy.ActionSize)
            {
                throw new MLAgentsException(
                    $"The heuristic provided does not match the action size. Expected {policy.ActionSize} but received {structSize} from heuristic");
            }
        }

        public void Process()
        {
            T action = m_Heuristic.Invoke();
            var totalCount = m_Policy.DecisionCounter.Count;

            // TODO : This can be parallelized
            if (m_Policy.ActionType == ActionType.CONTINUOUS)
            {
                var s = m_Policy.ContinuousActuators.Slice(0, totalCount * m_Policy.ActionSize).SliceConvert<T>();
                for (int i = 0; i < totalCount; i++)
                {
                    s[i] = action;
                }
            }
            else
            {
                var s = m_Policy.DiscreteActuators.Slice(0, totalCount * m_Policy.ActionSize).SliceConvert<T>();
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
