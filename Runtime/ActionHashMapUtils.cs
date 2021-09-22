using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.AI.MLAgents
{
    public static class ActionHashMapUtils
    {
        /// <summary>
        /// Retrieves the action data for a Policy in puts it into a HashMap.
        /// This action deletes the action data from the Policy.
        /// </summary>
        /// <param name="policy"> The Policy the data will be retrieved from.</param>
        /// <param name="allocator"> The memory allocator of the create NativeHashMap.</param>
        /// <typeparam name="T"> The type of the Action struct. It must match the Action Size
        /// and Action Type of the Policy.</typeparam>
        /// <returns> A NativeHashMap from Entities to Actions with type T.</returns>
        public static void GenerateActionHashMap<TC, TD>(
            this Policy policy,
            NativeHashMap<Entity, TC> continuousActionMap,
            NativeHashMap<Entity, TD> discreteActionMap)
            where TC : struct
            where TD : struct
        {
            int contSize = policy.ContinuousActionSize;
            int discSize = policy.DiscreteActionBranches.Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (contSize != UnsafeUtility.SizeOf<TC>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<TC>() / 4;
                throw new MLAgentsException($"Continuous action space size does not match for action. Expected {contSize} but received {receivedSize}");
            }
            if (discSize != UnsafeUtility.SizeOf<TD>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<TD>() / 4;
                throw new MLAgentsException($"Discrete action space size does not match for action. Expected {discSize} but received {receivedSize}");
            }
#endif

            for (int i = 0; i < policy.TerminationCounter.Count; i++)
            {
                // Remove the action of terminated agents
                continuousActionMap.Remove(policy.TerminationAgentEntityIds[i]);
                discreteActionMap.Remove(policy.TerminationAgentEntityIds[i]);
            }

            Academy.Instance.UpdatePolicy(policy);

            int actionCount = policy.ActionCounter.Count;

            for (int i = 0; i < actionCount; i++)
            {
                continuousActionMap.Remove(policy.ActionAgentEntityIds[i]);
                continuousActionMap.TryAdd(policy.ActionAgentEntityIds[i], policy.ContinuousActuators.Slice(i * contSize, contSize).SliceConvert<TC>()[0]);

                discreteActionMap.Remove(policy.ActionAgentEntityIds[i]);
                discreteActionMap.TryAdd(policy.ActionAgentEntityIds[i], policy.DiscreteActuators.Slice(i * discSize, discSize).SliceConvert<TD>()[0]);
            }
            policy.ResetActionsCounter();
        }

        /// <summary>
        /// Retrieves the continuous action data for a Policy in puts it into a HashMap.
        /// This action deletes the action data from the Policy.
        /// </summary>
        /// <param name="policy"> The Policy the data will be retrieved from.</param>
        /// <param name="allocator"> The memory allocator of the create NativeHashMap.</param>
        /// <typeparam name="T"> The type of the Action struct. It must match the Action Size
        /// and Action Type of the Policy.</typeparam>
        /// <returns> A NativeHashMap from Entities to Actions with type T.</returns>
        public static void GenerateContinuousActionHashMap<TC>(
            this Policy policy,
            NativeHashMap<Entity, TC> continuousActionMap)
            where TC : struct
        {
            int contSize = policy.ContinuousActionSize;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (contSize != UnsafeUtility.SizeOf<TC>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<TC>() / 4;
                throw new MLAgentsException($"Continuous action space size does not match for action. Expected {contSize} but received {receivedSize}");
            }
#endif

            for (int i = 0; i < policy.TerminationCounter.Count; i++)
            {
                // Remove the action of terminated agents
                continuousActionMap.Remove(policy.TerminationAgentEntityIds[i]);
            }

            Academy.Instance.UpdatePolicy(policy);

            int actionCount = policy.ActionCounter.Count;

            for (int i = 0; i < actionCount; i++)
            {
                continuousActionMap.Remove(policy.ActionAgentEntityIds[i]);
                continuousActionMap.TryAdd(policy.ActionAgentEntityIds[i], policy.ContinuousActuators.Slice(i * contSize, contSize).SliceConvert<TC>()[0]);
            }
            policy.ResetActionsCounter();
        }

        /// <summary>
        /// Retrieves the discrete action data for a Policy in puts it into a HashMap.
        /// This action deletes the action data from the Policy.
        /// </summary>
        /// <param name="policy"> The Policy the data will be retrieved from.</param>
        /// <param name="allocator"> The memory allocator of the create NativeHashMap.</param>
        /// <typeparam name="T"> The type of the Action struct. It must match the Action Size
        /// and Action Type of the Policy.</typeparam>
        /// <returns> A NativeHashMap from Entities to Actions with type T.</returns>
        public static void GenerateDiscreteActionHashMap<TD>(
            this Policy policy,
            NativeHashMap<Entity, TD> discreteActionMap)
            where TD : struct
        {
            int discSize = policy.DiscreteActionBranches.Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (discSize != UnsafeUtility.SizeOf<TD>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<TD>() / 4;
                throw new MLAgentsException($"Discrete action space size does not match for action. Expected {discSize} but received {receivedSize}");
            }
#endif

            for (int i = 0; i < policy.TerminationCounter.Count; i++)
            {
                // Remove the action of terminated agents
                discreteActionMap.Remove(policy.TerminationAgentEntityIds[i]);
            }

            Academy.Instance.UpdatePolicy(policy);

            int actionCount = policy.ActionCounter.Count;

            for (int i = 0; i < actionCount; i++)
            {
                discreteActionMap.Remove(policy.ActionAgentEntityIds[i]);
                discreteActionMap.TryAdd(policy.ActionAgentEntityIds[i], policy.DiscreteActuators.Slice(i * discSize, discSize).SliceConvert<TD>()[0]);
            }

            policy.ResetActionsCounter();
        }
    }
}
