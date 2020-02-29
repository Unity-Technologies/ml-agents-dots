using Unity.Collections;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.AI.MLAgents
{
    // TODO : A potential API to retrieve the actions on the main thread for projects allergic to jobs ?
    // TODO : Make faster and parallel
    internal static class ActionHashMapUtils
    {
        public static NativeHashMap<Entity, T> GetActionHashMap<T>(this MLAgentsWorld world, Allocator allocator) where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (world.ActionSize != UnsafeUtility.SizeOf<T>() / 4)
            {
                throw new MLAgentsException("Action space does not match for discrete action. Expected " + world.ActionSize);
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
            return result;
        }
    }
}
