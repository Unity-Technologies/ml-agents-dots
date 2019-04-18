using NUnit.Framework;
using DOTS_MLAgents;
using DOTS_MLAgents.Data;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;



namespace DOTS_MLAgents.Editor.Tests{
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
            var arr = new NativeArray<float>(8, Allocator.Temp);
            SensorToFloatUtility.StructToFloatArray(ref tmp, arr, 0);

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
            var tmp = new TestEnumSensor{
                data0 = Enumerator1.Value1,
                data1 = Enumerator2.Value2,
            };



            var arr = new NativeArray<float>(7, Allocator.Temp);
            SensorToFloatUtility.StructToFloatArray(ref tmp, arr, 0);
            Assert.AreEqual(1f,     arr[0]);
            Assert.AreEqual(0f,     arr[1]);
            Assert.AreEqual(0f,     arr[2]);
            Assert.AreEqual(0f,     arr[3]);
            Assert.AreEqual(1f,     arr[4]);
            Assert.AreEqual(0f,     arr[5]);
            Assert.AreEqual(0f,     arr[6]);

            arr.Dispose();

        }

        [Test]
        public void TestEnumAndFloatToFloat(){
            var tmp = new TestEnumAndFloatSensor{
                data0 = Enumerator1.Value2,
                data1 = new float3(3,2,1),
                data2 = Enumerator2.Value4,
                data3 = new float4(9,8,7,6)
            };



            var arr = new NativeArray<float>(28, Allocator.Temp);
            SensorToFloatUtility.StructToFloatArray(ref tmp, arr, 7);
            for (var i = 0; i<7 ; i++){
                Assert.AreEqual(0f,     arr[i]);
            }
            Assert.AreEqual(0f,     arr[7]);
            Assert.AreEqual(1f,     arr[8]);
            Assert.AreEqual(0f,     arr[9]);
            Assert.AreEqual(3f,     arr[10]);
            Assert.AreEqual(2f,     arr[11]);
            Assert.AreEqual(1f,     arr[12]);
            Assert.AreEqual(0f,     arr[13]);
            Assert.AreEqual(0f,     arr[14]);
            Assert.AreEqual(0f,     arr[15]);
            Assert.AreEqual(1f,     arr[16]);
            Assert.AreEqual(9f,     arr[17]);
            Assert.AreEqual(8f,     arr[18]);
            Assert.AreEqual(7f,     arr[19]);
            Assert.AreEqual(6f,     arr[20]);
            for (var i = 21; i<28 ; i++){
                Assert.AreEqual(0f,     arr[i]);
            }


            arr.Dispose();

        }

    }
}