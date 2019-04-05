using NUnit.Framework;
using ECS_MLAgents_v0;
using ECS_MLAgents_v0.Data;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS_MLAgents_v0.Editor.Tests{
    public class GenerateMetadata{

        const int SENSOR_METADATA_SIZE = 352;

        public struct TestSensor{
            float3 data0;
            [Sensor(SensorType.REWARD, "This is the reward")]
            float4 data1;
        }

        [Test]
        public void TestGetSensorMetaData(){
            var tmp = AttributeUtility.GetSensorMetaData(typeof(TestSensor));
            var metadata0 = tmp[0];
            Assert.AreEqual( SensorType.DATA,       metadata0.SensorType);
            Assert.AreEqual( "data0",               metadata0.Name.GetString());
            Assert.AreEqual( "",                    metadata0.Description.GetString());

            var metadata1 = tmp[1];
            Assert.AreEqual( SensorType.REWARD,     metadata1.SensorType);
            Assert.AreEqual( "data1",               metadata1.Name.GetString());
            Assert.AreEqual( "This is the reward",  metadata1.Description.GetString());

        }

        [Test]
        public void SensorMetadataSize(){
            var tmp = AttributeUtility.GetSensorMetaData(typeof(TestSensor));
            var metadata0 = tmp[0];
            Assert.AreEqual(SENSOR_METADATA_SIZE,   Marshal.SizeOf(metadata0));

        }
    }
}