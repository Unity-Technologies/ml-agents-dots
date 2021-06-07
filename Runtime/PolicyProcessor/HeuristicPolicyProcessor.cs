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
        /// <param name="continuousHeuristic"> The Heuristic used to generate the continuous actions.
        /// Note that all agents in the Policy will receive the same action.</param>
        /// <param name="discreteHeuristic"> The Heuristic used to generate the discrete actions.
        /// Note that all agents in the Policy will receive the same action.</param>
        /// <typeparam name="TC"> The type of the continuous Action struct. It must match the Action
        /// Size of the continuous action. If there are no continuous actions, set to an empty struct.</typeparam>
        /// <typeparam name="TD"> The type of the discrete Action struct. It must match the Action
        /// Size of the discrete action. If there are no discrete actions, set to an empty struct.</typeparam>
        public static void RegisterPolicyWithHeuristic<TC, TD>(
            this Policy policy,
            string policyId,
            Func<TC> continuousHeuristic = null,
            Func<TD> discreteHeuristic = null
        ) where TC : struct
            where TD : struct
        {
            var policyProcessor = new HeuristicPolicyProcessor<TC, TD>(policy, continuousHeuristic, discreteHeuristic);
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
        /// <param name="continuousHeuristic"> The Heuristic used to generate the continuous actions.
        /// Note that all agents in the Policy will receive the same action.</param>
        /// <param name="discreteHeuristic"> The Heuristic used to generate the discrete actions.
        /// Note that all agents in the Policy will receive the same action.</param>
        /// <typeparam name="TC"> The type of the continuous Action struct. It must match the Action
        /// Size of the continuous action. If there are no continuous actions, set to an empty struct.</typeparam>
        /// <typeparam name="TD"> The type of the discrete Action struct. It must match the Action
        /// Size of the discrete action. If there are no discrete actions, set to an empty struct.</typeparam>
        public static void RegisterPolicyWithHeuristicForceNoCommunication<TC, TD>(
            this Policy policy,
            string policyId,
            Func<TC> continuousHeuristic = null,
            Func<TD> discreteHeuristic = null
        ) where TC : struct
            where TD : struct
        {
            var policyProcessor = new HeuristicPolicyProcessor<TC, TD>(policy, continuousHeuristic, discreteHeuristic);
            Academy.Instance.RegisterPolicy(policyId, policy, policyProcessor, false);
        }
    }

    internal class HeuristicPolicyProcessor<TC, TD> : IPolicyProcessor where TC : struct
        where TD : struct
    {
        private Func<TC> m_ContinuousHeuristic;
        private Func<TD> m_DiscreteHeuristic;
        private Policy m_Policy;

        public bool IsConnected {get {return false;}}

        internal HeuristicPolicyProcessor(Policy policy, Func<TC> continuousHeuristic = null, Func<TD> discreteHeuristic = null)
        {
            this.m_Policy = policy;
            this.m_ContinuousHeuristic = continuousHeuristic;
            this.m_DiscreteHeuristic = discreteHeuristic;
            if (m_ContinuousHeuristic != null)
            {
                var structSize = UnsafeUtility.SizeOf<TC>() / sizeof(float);
                if (structSize != policy.ContinuousActionSize)
                {
                    throw new MLAgentsException(
                        $"The continuous heuristic provided does not match the continuous action size. Expected {policy.ContinuousActionSize} but received {structSize} from heuristic");
                }
            }
            if (m_DiscreteHeuristic != null)
            {
                var structSize = UnsafeUtility.SizeOf<TD>() / sizeof(int);
                if (structSize != policy.DiscreteActionBranches.Length)
                {
                    throw new MLAgentsException(
                        $"The discrete heuristic provided does not match the discrete action size. Expected {policy.DiscreteActionBranches.Length} but received {structSize} from heuristic");
                }
            }
        }

        public void Process()
        {
            var totalCount = m_Policy.DecisionCounter.Count;
            if (m_ContinuousHeuristic != null)
            {
                TC action = m_ContinuousHeuristic.Invoke();
                var s = m_Policy.ContinuousActuators.Slice(0, totalCount * m_Policy.ContinuousActionSize).SliceConvert<TC>();
                for (int i = 0; i < totalCount; i++)
                {
                    s[i] = action;
                }
            }
            if (m_DiscreteHeuristic != null)
            {
                TD action = m_DiscreteHeuristic.Invoke();
                var s = m_Policy.DiscreteActuators.Slice(0, totalCount * m_Policy.DiscreteActionBranches.Length).SliceConvert<TD>();
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
