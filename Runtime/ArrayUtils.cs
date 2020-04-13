using Unity.Collections;
using Unity.Mathematics;
using System.Text;

namespace Unity.AI.MLAgents
{
    internal static class ArrayUtils
    {
        public static int Sum(this NativeArray<int> array)
        {
            int result = 0;
            for (int i = 0; i < array.Length; i++)
            {
                result += array[i];
            }
            return result;
        }

        /// <summary>
        /// Returns the sum of all the values of an integer NativeArray that are strictly before
        /// a certain index.
        /// For example, if index is 0, then the output is always 0
        /// If index it 2, then the result is the sum of the first 2 values in the array
        /// </summary>
        public static int CumSumAt(this NativeArray<int> array, int index)
        {
            int result = 0;
            for (int i = 0; i < index; i++)
            {
                result += array[i];
            }
            return result;
        }

        public static int IncreaseArraySizeHeuristic(int newSize)
        {
            return newSize * 2 + 20;
        }

        public static int GetTotalTensorSize(this int3 tensorShape)
        {
            return tensorShape.x * math.max(1, tensorShape.y) * math.max(1, tensorShape.z);
        }

        public static int GetDimensions(this int3 tensorShape)
        {
            if (tensorShape.y == 0)
            {
                return 1;
            }
            if (tensorShape.z == 0)
            {
                return 2;
            }
            return 3;
        }
    }
}


/*

/// <summary>
        /// Computes the total number of bytes necessary in the shared memory file to
        /// send and receive data for a specific MLAgentsWorld.
        /// </summary>
        public static int GetRequiredCapacity(MLAgentsWorld world)
        {
            // # int : 4 bytes : maximum number of Agents
            // # bool : 1 byte : is action discrete (False) or continuous (True)
            // # int : 4 bytes : action space size (continuous) / number of branches (discrete)
            // # -- If discrete only : array of action sizes for each branch
            // # int : 4 bytes : number of observations
            // # For each observation :
            // # 3 int : shape (the shape of the tensor observation for one agent
            // # start of the section that will change every step
            // # 4 bytes : n_agents at current step
            // # ? Bytes : the data : obs,reward,done,max_step,agent_id,masks,action
            int capacity = 64; // Name
            capacity += 4; // N Max Agents
            capacity += 1; // discrete or continuous
            capacity += 4; // action Size
            if (world.ActionType == ActionType.DISCRETE)
            {
                capacity += 4 * world.ActionSize; // The action branches
            }
            capacity += 4; // number of observations
            capacity += 3 * 4 * world.SensorShapes.Length; // The observation shapes
            capacity += 4; // Number of agents for the current step

            var nAgents = world.Rewards.Length;
            capacity += 4 * world.Sensors.Length;
            capacity += 4 * nAgents;
            capacity += nAgents;
            capacity += nAgents;
            capacity += 4 * nAgents;
            if (world.ActionType == ActionType.DISCRETE)
            {
                foreach (int branch_size in world.DiscreteActionBranches)
                {
                    capacity += branch_size * nAgents;
                }
            }
            capacity += 4 * world.ActionSize * nAgents;

            return capacity;
        }
        */
