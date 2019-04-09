using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.InteropServices;


namespace ECS_MLAgents_v0.Data
{
    // This is dangerous because if the metadata is wrong, you can get out of memory
    public unsafe static class SensorToFloatUtility {
        public static void StructToFloatArray<T>(ref T src,
        NativeArray<float> dst,
        //IMetaData[]/*NativeArray ? Stateful ?*/ metaData,
        int offset)
        where T:struct {

            SensorMetadata[] metaData = AttributeUtility.GetSensorMetaData(typeof(T));
            int arrayOffset = offset;
            int structOffset = 0;
            void* TPtr = UnsafeUtility.AddressOf(ref src);
            foreach (SensorMetadata m in metaData){
                int dimension = m.Dimension.x;
                switch(m.DataType){
                    case DataType.FLOAT:
                        // UnsafeUtility.MemCpy(
                        //      (byte*)TPtr + structOffset*4, (byte*)dst.GetUnsafePtr() + arrayOffset*4, dimension*4);
                        Buffer.MemoryCopy(
                            (byte*)TPtr + structOffset*4, (byte*)dst.GetUnsafePtr() + arrayOffset*4, dimension*4 ,dimension*4);

                        arrayOffset += dimension;
                        structOffset+= dimension;
                        break;
                    case DataType.ENUM: // This would only work with int enums
                        var slice = dst.Slice(arrayOffset, dimension);
                        for(var i=0; i<dimension;i++){
                            slice[i] = 0f;
                        }
                        var enumValue = (int)(IntPtr)((byte*)TPtr + structOffset * 4);
                        enumValue =Marshal.ReadInt32((IntPtr)((byte*)TPtr + structOffset*4));
                        if (enumValue > dimension){
                            throw new NotImplementedException("Enum value too large");
                        }
                        slice[enumValue] = 1f;

                        arrayOffset += dimension;
                        structOffset+= 1;
                        break;
                    default:
                        throw new NotImplementedException("Unsupported DataType");
                }

            }
        }

        public static void FloatArrayToActuator<T>(NativeArray<float> src, T dst, int offset){

        }
    }

}