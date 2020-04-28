using NUnit.Framework;
using UnityEngine;
using System.IO;
using Unity.AI.MLAgents;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestSharedMemory
    {
        [Test]
        public void TestBaseSharedMemory()
        {
            var filePath = Path.Combine(Path.GetTempPath(), "ml-agents", "test");
            File.Delete(filePath);

            var sm0 = new BaseSharedMemory("test", true, 256);
            var sm1 = new BaseSharedMemory("test", false);

            var newOffset = sm0.SetInt(0, 3);
            Assert.AreEqual(newOffset, sizeof(int));
            Assert.AreEqual(3, sm1.GetInt(0));

            newOffset = sm0.SetFloat(0, 3f);
            Assert.AreEqual(newOffset, sizeof(float));
            Assert.AreEqual(3f, sm1.GetFloat(0));

            newOffset = sm0.SetBool(0, true);
            Assert.AreEqual(newOffset, sizeof(bool));
            Assert.AreEqual(true, sm1.GetBool(0));

            newOffset = sm0.SetString(0, "foo");
            Assert.AreEqual("foo", sm1.GetString(0));
            sm1.SetString(newOffset, "bar");
            Assert.AreEqual("bar", sm0.GetString(newOffset));

            Assert.AreEqual(sm0.GetBytes(0, 8), sm1.GetBytes(0, 8));

            var src = new NativeArray<int4>(3, Allocator.Temp);
            src[1] = new int4(1, 2, 3, 4);
            sm0.SetArray(0, src, 3 * 16);
            var dst = new NativeArray<int4>(3, Allocator.Temp);
            sm1.GetArray(0, dst, 3 * 16);
            Assert.AreEqual(dst[1], new int4(1, 2, 3, 4));
            src.Dispose();
            dst.Dispose();

            sm0.Close();

            Assert.True(File.Exists(filePath));
            sm1.Delete();
            Assert.False(File.Exists(filePath));
        }
    }
}
