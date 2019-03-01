using System;
using Unity.Collections;
using Unity.Jobs;
using System.IO.MemoryMappedFiles;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;


namespace ECS_MLAgents_v0.Core{
    public class ExternalDecision : IAgentDecision
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
        
        
        private float[] actuatorData = new float[0];
        
        // This is a temporary test file
        // TODO : Replace with a file creation system
        // TODO : Implement the communication in a separate class
        // TODO : Have separate files for sensor and actuators
        private string filenameWrite = "Assets/shared_communication_file.txt";
        
        private MemoryMappedViewAccessor accessor;
        
        public ExternalDecision()
        {
            var mmf = MemoryMappedFile.CreateFromFile(filenameWrite, FileMode.Open, "Test");
            accessor = mmf.CreateViewAccessor(
                0, FILE_CAPACITY, MemoryMappedFileAccess.ReadWrite);
//            accessor.WriteArray(0, new bool[FILE_CAPACITY], 0, FILE_CAPACITY);
            accessor.Write(PYTHON_READY_POSITION, false);
            accessor.Write(UNITY_READY_POSITION, false);
            Debug.Log("Is Ready to Communicate");
        }
        
        public JobHandle DecideBatch(
            ref NativeArray<float> sensor, 
            ref NativeArray<float> actuator, 
            int sensorSize,
            int actuatorSize, 
            int nAgents, 
            JobHandle handle)
        {
            Profiler.BeginSample("Communicating");
            if (sensor.Length > 4 * 50000)
            {
                throw new Exception("TOO much data to send");
            }
            
            if (actuator.Length > 4 * 50000)
            {
                throw new Exception("TOO much data to send");
            }
            
            if (actuatorData.Length < actuator.Length)
            {
                actuatorData = new float[actuator.Length];
            }
            
            
            accessor.Write(NUMBER_AGENTS_POSITION, nAgents);
            accessor.Write(SENSOR_SIZE_POSITION, sensorSize);
            accessor.Write(ACTUATOR_SIZE_POSITION, actuatorSize);
            
            accessor.WriteArray(SENSOR_DATA_POSITION, sensor.ToArray(), 0, sensor.Length);
            
            accessor.Write(PYTHON_READY_POSITION, false);
            
            accessor.Write(UNITY_READY_POSITION, true);
            
            
            var readyToContinue = false;
            int loopIter = 0;
            while (!readyToContinue)
            {
                loopIter++;
                readyToContinue = accessor.ReadBoolean(PYTHON_READY_POSITION);
                readyToContinue = readyToContinue || loopIter > 200000;
                if (loopIter > 200000)
                {
                    Debug.Log("Missed Communication");
                }
            }

            accessor.ReadArray(ACTUATOR_DATA_POSITION, actuatorData, 0, actuator.Length);
            actuator.CopyFrom(actuatorData);

            Profiler.BeginSample("Communicating");
            return handle;
        }
    }
}
