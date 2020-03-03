using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.AI.MLAgents
{
    // TODO : A potential API to retrieve the actions on the main thread for projects allergic to jobs ?
    // TODO : Make faster and parallel
    internal static class ActionHashMapUtils
    {
        /// <summary>
        /// Retrieves the action data for a world in puts it into a HashMap
        /// </summary>
        /// <param name="world"> The MLAgentsWorld the data will be retrieved from.</param>
        /// <param name="allocator"> The memory allocator of the create NativeHashMap.</param>
        /// <typeparam name="T"> The type of the Action struct. It must match the Action Size and Action Type of the world.</typeparam>
        /// <returns> A NativeHashMap from Entities to Action.</returns>
        public static NativeHashMap<Entity, T> GetActionHashMap<T>(this MLAgentsWorld world, Allocator allocator) where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (world.ActionSize != UnsafeUtility.SizeOf<T>() / 4)
            {
                var receivedSize = UnsafeUtility.SizeOf<T>() / 4;
                throw new MLAgentsException($"Action space size does not match for action. Expected {world.ActionSize} but received {receivedSize}");
            }
#endif
            Academy.Instance.UpdateWorld(world);
            int actionCount = world.ActionCounter.Count;
            var result = new NativeHashMap<Entity, T>(actionCount, allocator);
            int size = world.ActionSize;
            for (int i = 0; i < actionCount; i++)
            {
                if (!world.ActionDoneFlags[i])
                {
                    if (world.ActionType == ActionType.DISCRETE)
                    {
                        result.TryAdd(world.ActionAgentIds[i], world.DiscreteActuators.Slice(i * size, size).SliceConvert<T>()[0]);
                    }
                    else
                    {
                        result.TryAdd(world.ActionAgentIds[i], world.ContinuousActuators.Slice(i * size, size).SliceConvert<T>()[0]);
                    }
                }
            }
            world.ResetActionsCounter();
            return result;
        }
    }
}
