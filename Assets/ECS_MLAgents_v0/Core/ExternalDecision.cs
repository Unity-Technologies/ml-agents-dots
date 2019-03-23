using System;
using Unity.Collections;
using Unity.Jobs;
using System.IO.MemoryMappedFiles;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Entities;


namespace ECS_MLAgents_v0.Core{
    public class ExternalDecision<TS, TA> : IAgentDecision<TS, TA> 
        where TS : struct, IComponentData
        where TA : struct, IComponentData 
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



        System.Type _sensorType;
        System.Type _actuatorType;

        private int _sensorSize;
        private int _actuatorSize;
        
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
        
         public void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<TA> actuators )
        {
            Profiler.BeginSample("Communicating");

            VerifySensor(typeof(TS));
            VerifyActuator(typeof(TA));

            int batch = sensors.Length;
            if (batch != actuators.Length)
            {
                throw new Exception("Error in the length of the sensors and actuators");
            }

            if (batch > 50000)
            {
                throw new Exception("TOO much data to send");
            }
            
            if (actuatorData.Length < _actuatorSize* batch)
            {
                actuatorData = new float[_actuatorSize * batch];
            }
            
            
            accessor.Write(NUMBER_AGENTS_POSITION, batch);
            accessor.Write(SENSOR_SIZE_POSITION, _sensorSize);
            accessor.Write(ACTUATOR_SIZE_POSITION, _actuatorSize);
            
            accessor.WriteArray(SENSOR_DATA_POSITION, sensors.ToArray(), 0, batch);
            
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

            accessor.ReadArray(ACTUATOR_DATA_POSITION, actuatorData, 0, batch * _actuatorSize);

            // actuator.CopyFrom(actuatorData);

            var tmpA = new NativeArray<float>(batch * _actuatorSize, Allocator.Persistent);
            tmpA.CopyFrom(actuatorData);
            for(var i = 0; i< batch; i++){
                var act = new TA();
                TensorUtility.CopyFromNativeArray(tmpA, out act, i * _sensorSize * 4);
                actuators[i] = act;
            }
            tmpA.Dispose();


            Profiler.BeginSample("Communicating");
        }

        private void VerifySensor(System.Type t){
            if (! t.Equals(_sensorType)){
                TensorUtility.DebugCheckStructure(t);
                _sensorSize = System.Runtime.InteropServices.Marshal.SizeOf(t) / 4;
                _sensorType = t;
            }
        }
        private void VerifyActuator(System.Type t){
            if (! t.Equals(_actuatorType)){
                TensorUtility.DebugCheckStructure(t);
                _actuatorSize = System.Runtime.InteropServices.Marshal.SizeOf(t) / 4;
                _actuatorType = t;
            }
        }
    }
}
