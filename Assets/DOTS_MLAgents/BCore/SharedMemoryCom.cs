using System;
using Unity.Collections;
using Unity.Jobs;
using System.IO.MemoryMappedFiles;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Entities;
using System.IO;
using System.Runtime.InteropServices;


namespace DOTS_MLAgents.Core
{
    public unsafe class SharedMemoryCom : IDisposable
    {

        // [ Unity Ready (1) , nAgents (4) , sensorSize (4) , actuatorSize (4) , Data

        // TODO : This capacity needs to scale / communicate multiple times per step ?
        private const int FILE_CAPACITY = 200000;
        private const int NUMBER_AGENTS_POSITION = 0;
        private const int SENSOR_SIZE_POSITION = 4;
        private const int ACTUATOR_SIZE_POSITION = 8;
        private const int UNITY_READY_POSITION = 12;
        private const int SENSOR_DATA_POSITION = 13;

        private const int PYTHON_READY_POSITION = 100000;
        private const int ACTUATOR_DATA_POSITION = 100001;






        private MemoryMappedViewAccessor accessor;

        public SharedMemoryCom(string fileId)
        {
            var fileName = Path.Combine(Path.GetTempPath(), fileId);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            var f = File.Create(fileName, FILE_CAPACITY);
            f.Write(new byte[FILE_CAPACITY], 0, FILE_CAPACITY);
            f.Close();

            var mmf = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open);
            accessor = mmf.CreateViewAccessor(
                0, FILE_CAPACITY, MemoryMappedFileAccess.ReadWrite);
            mmf.Dispose();

            accessor.Write(PYTHON_READY_POSITION, false);
            accessor.Write(UNITY_READY_POSITION, false);
            Debug.Log("Is Ready to Communicate");

        }

        public void WriteWorld(MLAgentsWorld world)
        {
            if (world.AgentCounter.Count > 100)
            {
                throw new Exception("TOO much data to send");
            }

            accessor.Write(NUMBER_AGENTS_POSITION, world.AgentCounter.Count);
            accessor.Write(SENSOR_SIZE_POSITION, world.SensorFloatSize);
            accessor.Write(ACTUATOR_SIZE_POSITION, world.ActuatorFloatSize);
            byte* ptr = (byte*)0;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            IntPtr dst = IntPtr.Add(new IntPtr(ptr), SENSOR_DATA_POSITION);
            IntPtr src = new IntPtr(world.Sensors.GetUnsafePtr());
            int length = world.AgentCounter.Count * world.SensorFloatSize * sizeof(float);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
            // accessor.WriteArray(SENSOR_DATA_POSITION, sensors.Slice(offset, batch).ToArray(), 0, batch);

        }

        public void Advance()
        {
            accessor.Write(PYTHON_READY_POSITION, false);
            accessor.Write(UNITY_READY_POSITION, true);
            var readyToContinue = false;
            int loopIter = 0;
            while (!readyToContinue)
            {
                loopIter++;
                readyToContinue = accessor.ReadBoolean(PYTHON_READY_POSITION);
                readyToContinue = readyToContinue || loopIter > 20000000;
                if (loopIter > 20000000)
                {
                    Debug.Log("Missed Communication");
                }
            }
        }

        public void LoadWorld(MLAgentsWorld world)
        {
            // accessor.ReadArray(ACTUATOR_DATA_POSITION, actuatorData, 0, batch);
            // actuators.Slice(offset, batch).CopyFrom(actuatorData);
            byte* ptr = (byte*)0;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            IntPtr src = IntPtr.Add(new IntPtr(ptr), ACTUATOR_DATA_POSITION);
            IntPtr dst = new IntPtr(world.Actuators.GetUnsafePtr());
            int length = world.AgentCounter.Count * world.ActuatorFloatSize * sizeof(float);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
        }

        public void Dispose()
        {
            accessor.Dispose();
        }
    }
}
