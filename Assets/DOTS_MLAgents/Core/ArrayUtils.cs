using Unity.Collections;

namespace DOTS_MLAgents.Core
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

        public static int CumSumAt(this NativeArray<int> array, int index)
        {
            int result = 0;
            for (int i = 0; i < index; i++)
            {
                result += array[i];
            }
            return result;
        }
    }
}