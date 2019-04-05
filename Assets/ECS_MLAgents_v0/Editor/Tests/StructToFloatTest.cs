using NUnit.Framework;
using ECS_MLAgents_v0;
using ECS_MLAgents_v0.Data;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ECS_MLAgents_v0.Editor.Tests{
    public class StructToFloatTest{

        public enum Enumerator1 {
            Value1,
            Value2,
            Value3
        }
        public enum Enumerator2 {
            Value1,
            Value2,
            Value3,
            Value4
        }

        public struct TestFloatSensor{
            public float3 data0;
            float4 data1;
            public void setData1(float4 d){
                data1 = d;
            }
        }
                
        public struct TestEnumSensor{
            public Enumerator1 data0;
            public Enumerator2 data1;
        }

        public struct TestEnumAndFloatSensor{
            public Enumerator1 data0;
            public float3 data1;
            public Enumerator2 data2;
            public float4 data3;
        }

        [Test]
        public void TestFloatToFloat(){
            var tmp = new TestFloatSensor{
                data0 = new float3(1,2,3),
            };
            tmp.setData1(new float4(4,5,6,7));
            var arr = new NativeArray<float>(8, Allocator.Persistent);
            SensorToFloatUtility.StructToFloatArray(ref tmp, arr, 0);

            for (var i = 0; i< 8; i++){
                Debug.Log(arr[i]);
            }
            Debug.Log(tmp.data0);

            Assert.AreEqual(1f,     arr[0]);
            Assert.AreEqual(2f,     arr[1]);
            Assert.AreEqual(3f,     arr[2]);
            Assert.AreEqual(4f,     arr[3]);
            Assert.AreEqual(5f,     arr[4]);
            Assert.AreEqual(6f,     arr[5]);
            Assert.AreEqual(7f,     arr[6]);
            Assert.AreEqual(0f,     arr[7]);

            arr.Dispose();
        }


        [Test]
        public void TestEnumToFloat(){


        }

        [Test]
        public void TestEnumAndFloatToFloat(){


        }

    }
}