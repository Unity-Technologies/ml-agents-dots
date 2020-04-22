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
