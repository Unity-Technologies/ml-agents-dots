using System;
using Unity.Collections;
using Unity.Jobs;
using System.IO.MemoryMappedFiles;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Entities;


namespace DOTS_MLAgents.Core{
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
        
        
        private TA[] actuatorData = new TA[0];



        System.Type _sensorType;
        System.Type _actuatorType;

        private int _sensorSize;
        private int _actuatorSize;
        
        // This is a temporary test file
        // TODO : Replace with a file creation system
        // TODO : Implement the communication in a separate class
        // TODO : Have separate files for sensor and actuators
        // private string filenameWrite = "Assets/shared_communication_file.txt";
        private string filenameWrite = "shared_communication_file.txt";


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
        
         public void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<TA> actuators, int offset = 0, int size = -1)
        {
            Profiler.BeginSample("__Communicating");

            Profiler.BeginSample("__TypeCheck");
            VerifySensor(typeof(TS));
            VerifyActuator(typeof(TA));
            if (size ==-1){
                size = sensors.Length - offset;
            }
            Profiler.EndSample();

            Profiler.BeginSample("__VerifyLength");
            int batch = size;
            if (sensors.Length != actuators.Length)
            {
                throw new Exception("Error in the length of the sensors and actuators");
            }

            if (batch > 50000)
            {
                throw new Exception("TOO much data to send");
            }
            
            if (actuatorData.Length < _actuatorSize* batch)
            {
                actuatorData = new TA[batch];
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("__Write");
            accessor.Write(NUMBER_AGENTS_POSITION, batch);
            accessor.Write(SENSOR_SIZE_POSITION, _sensorSize);
            accessor.Write(ACTUATOR_SIZE_POSITION, _actuatorSize);
            
            accessor.WriteArray(SENSOR_DATA_POSITION, sensors.Slice(offset, batch).ToArray(), 0, batch);
            
            accessor.Write(PYTHON_READY_POSITION, false);
            
            accessor.Write(UNITY_READY_POSITION, true);
            Profiler.EndSample();
            
            Profiler.BeginSample("__Wait");
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
            Profiler.EndSample();

            Profiler.BeginSample("__Read");
            accessor.ReadArray(ACTUATOR_DATA_POSITION, actuatorData, 0, batch);

            actuators.Slice(offset, batch).CopyFrom(actuatorData);

            // for(var i = 0; i< batch; i++){
            //     actuators[i] = actuatorData[i];
            // }

            Profiler.EndSample();
            Profiler.EndSample();

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
