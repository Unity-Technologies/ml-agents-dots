using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Core
{
    /*
     * A library that uses unsafe code to copy data between structs and NativeArrays.
     */
    public static class TensorUtility
    {
        // Replace this with a set
        private static readonly List<Type> SeenTypes = new List<Type>();
        
        /// <summary>
        /// Copies a blittable struct of float data into a NativeArray of floats at a specific
        /// location.
        /// </summary>
        /// <param name="src"> The source struct that contains the data to be copied</param>
        /// <param name="dst"> The destination NativeArray of floats that will receive the data
        /// </param>
        /// <param name="index"> The index in the NativeArray destination at which to copy the data
        /// </param>
        /// <typeparam name="T"> The Type of the struct that will be copied.</typeparam>
        public static void CopyToNativeArray<T>(T src, NativeArray<float> dst, int index)
            where T : struct
        {
            if (!SeenTypes.Contains(typeof(T)))
            {
                DebugCheckStructure(typeof(T));
            }
            unsafe
            {
                UnsafeUtility.CopyStructureToPtr<T>(ref src, (byte*) (dst.GetUnsafePtr()) + index);
            }
        }
        
        /// <summary>
        /// Copies the content of a NativeArray of float at a specific location into a blittable
        /// struct of float.
        /// </summary>
        /// <param name="src"> The source NativeArray that contains the data to be copied.</param>
        /// <param name="dst"> The destination struct that will receive the data</param>
        /// <param name="index"> The index in the NativeArray at which the data is located.</param>
        /// <typeparam name="T"> The Type of the struct that will receive the data</typeparam>
        public static void CopyFromNativeArray<T>(NativeArray<float> src, out T dst, int index)
            where T : struct
        {
            if (!SeenTypes.Contains(typeof(T)))
            {
                DebugCheckStructure(typeof(T));
            }
            unsafe
            {
                UnsafeUtility.CopyPtrToStructure((byte*) (src.GetUnsafePtr()) + index, out dst);
            }
        }
        
        /// <summary>
        /// A helper method that checks if the type of a struct is supported by the library. The
        /// struct must be blittable and only contain fields of float with a valid type.
        /// </summary>
        /// <param name="t"> The Type that will be checked</param>
        /// <exception cref="NotSupportedException"> NotSupportedException will be raised if the
        /// Type t is not valid for use by the library.</exception>
        private static void DebugCheckStructure(Type t)
        {
            SeenTypes.Add(t);
            if (t.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Any(f => !IsCompatibleObservationFieldType(f.FieldType)))
            {
                throw new NotSupportedException(
                    "You are trying to add an struct as observation data which contains an " +
                    "incompatible member type. Only float and vectors are supported for " +
                    "struct observations");
            }
        }

        /// <summary>
        /// Helper method that checks if the type of a field is a compatible blittable float.
        /// </summary>
        /// <param name="t"> The Type of the field.</param>
        /// <returns> True if the Type is compatible and false otherwise.</returns>
        private static bool IsCompatibleObservationFieldType(Type t)
        {
            if (t == typeof(float))
                return true;
            if (t == typeof(float2))
                return true;
            if (t == typeof(float3))
                return true;
            if (t == typeof(float4))
                return true;
            if (t == typeof(quaternion))
                return true;
            if (t == typeof(float2x2))
                return true;
            if (t == typeof(float3x3))
                return true;
            if (t == typeof(float4x4))
                return true;
            return false;
        }
    }
    
}
