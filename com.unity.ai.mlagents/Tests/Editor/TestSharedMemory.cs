using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using System.IO;
using System;
using System.IO.MemoryMappedFiles;

namespace Unity.AI.MLAgents.Tests.Editor
{
    public class TestSharedMemory
    {

        public string GenerateSMFile(string fileId = "TEST", byte[] sideChannelData = null, sbyte command = 0)
        {
            if (!Directory.Exists(Path.Combine(Path.GetTempPath(), "ml-agents")))
            {
                Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ml-agents"));
            }
            var path = Path.Combine(Path.GetTempPath(), "ml-agents", fileId);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                var datalen = 0;
                if (sideChannelData != null)
                {
                    datalen = sideChannelData.Length;
                }

                fs.Write(BitConverter.GetBytes(18 + datalen), 0, 4);
                fs.Write(BitConverter.GetBytes(0), 0, 4);
                fs.Write(BitConverter.GetBytes(true), 0, 1);
                fs.Write(BitConverter.GetBytes(command), 0, 1);
                if (sideChannelData != null)
                {
                    fs.Write(BitConverter.GetBytes(datalen + 4), 0, 4); // MaxCapacity
                    fs.Write(BitConverter.GetBytes(datalen), 0, 4); // Data Size
                    fs.Write(sideChannelData, 0, datalen);
                }
                else
                {
                    fs.Write(BitConverter.GetBytes(0), 0, 4);
                }
                fs.Write(BitConverter.GetBytes(0), 0, 4);

            }
            return path;
        }


        [Test]
        public void TestCreation()
        {
            var path = GenerateSMFile();
            var sm = new SharedMemoryCom(path);
            sm.Dispose();
            File.Delete(path);
        }

        [Test]
        public void TestChangeFile()
        {
            var path1 = GenerateSMFile("TEST", null, 2); // change file
            var path2 = GenerateSMFile("TEST_", BitConverter.GetBytes(2020), 0); //default

            var mmf = MemoryMappedFile.CreateFromFile(path1, FileMode.Open);
            var accessor = mmf.CreateViewAccessor(0, 18, MemoryMappedFileAccess.ReadWrite);
            Debug.Log(accessor.ReadSByte(9));

            var sm = new SharedMemoryCom(path1);
            sm.Advance();


            var data = sm.ReadAndClearSideChannelData();
            Assert.AreEqual(BitConverter.GetBytes(2020), data);
            Assert.False(File.Exists(path1));
            sm.Dispose();

            File.Delete(path2);
        }

        [Test]
        public void TestWriteWorld()
        {
            var path1 = GenerateSMFile("TEST", null, 0); // Default

            var sm = new SharedMemoryCom(path1);

            var uCommand = sm.Advance();
            Assert.AreEqual(SharedMemoryCom.PythonCommand.DEFAULT, uCommand);

            var w = new MLAgentsWorld(10, ActionType.CONTINUOUS, new int3[1] { new int3(3, 0, 0) }, 3, null);
            for (int i = 0; i < 10; i++)
            {
                // w.Rewards[i] = i;
                w.RequestDecision(Entity.Null).SetReward(i);
            }
            sm.WriteWorld("test", w);

            var mmf = MemoryMappedFile.CreateFromFile(path1, FileMode.Open);
            var accessor = mmf.CreateViewAccessor(0, 18, MemoryMappedFileAccess.ReadWrite);
            Assert.AreEqual((sbyte)SharedMemoryCom.PythonCommand.CHANGE_FILE, accessor.ReadSByte(9));
            Assert.False(accessor.ReadBoolean(8));
            accessor.Dispose();
            mmf.Dispose();

            var path2 = path1 + "_";
            Assert.True(File.Exists(path2));


            mmf = MemoryMappedFile.CreateFromFile(path2, FileMode.Open);
            accessor = mmf.CreateViewAccessor(0, 18, MemoryMappedFileAccess.ReadWrite);
            var capacity = accessor.ReadInt32(0);
            accessor.Dispose();
            accessor = mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite);
            Assert.Greater(capacity, 18 + 3 * 4 * 10); // File was extended

            sm.Dispose();
            File.Delete(path1);
            File.Delete(path2);
            w.Dispose();

        }

        [Test]
        public void TestCloseCommand()
        {

        }

        [Test]
        public void TestExtendSideChannel()
        {

        }

        [Test]
        public void TestLoadWorld()
        {

        }

    }
}