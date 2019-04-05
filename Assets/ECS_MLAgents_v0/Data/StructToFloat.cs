using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Mathematics;
using ECS_MLAgents_v0.Editor.Tests;

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



            // var ttt = (ECS_MLAgents_v0.Editor.Tests.StructToFloatTest.TestFloatSensor) TPtr;

            int arrayOffset = offset;
            int structOffset = 0;
            void* TPtr = UnsafeUtility.AddressOf(ref src);


            // ECS_MLAgents_v0.Editor.Tests.StructToFloatTest.TestFloatSensor ttt = (ECS_MLAgents_v0.Editor.Tests.StructToFloatTest.TestFloatSensor) &TPtr;

            foreach (SensorMetadata m in metaData){
                int dimension = m.Dimension.x;
                switch(m.DataType){
                    case DataType.FLOAT:
                        Debug.Log("FLOAT "+dimension);
                        UnsafeUtility.MemMove(
                            (byte*)TPtr + structOffset, (byte*)dst.GetUnsafePtr() + arrayOffset, dimension);

for (var i = 0; i< 8; i++){
                Debug.Log(dst[i]+"  "+ ((byte*)TPtr)[i].ToString());
            }
                        arrayOffset += dimension;
                        structOffset+= dimension;
                        break;
                    case DataType.ENUM: // This would only work with int enums
                        var slice = dst.Slice(arrayOffset, dimension);
                        for(var i=0; i<dimension;i++){
                            slice[i] = 0f;
                        }
                        var enumValue = (int)((byte*)TPtr + structOffset);
                        if (enumValue > dimension){
                            throw new NotImplementedException("Enum value too large");
                        }
                        slice[enumValue] = 1f;
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