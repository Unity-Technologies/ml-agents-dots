using Unity.Entities;
using Barracuda;
using Unity.Collections;
using ECS_MLAgents_v0.Core.Inference;
using ECS_MLAgents_v0.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace ECS_MLAgents_v0.Core {
    public class NNDecision<TS, TA> : IAgentDecision<TS, TA> 
        where TS : struct, IComponentData
        where TA : struct, IComponentData 
    {

        private const int INITIAL_MEMORY_SIZE = 1024;

        private const int SIZE_OF_FLOAT_IN_MEMORY = 4;
        private NNModel _model; 
        public InferenceDevice inferenceDevice = InferenceDevice.CPU;
        private Model _barracudaModel;
        private IWorker _engine;
        private const bool _verbose = false;

        System.Type _sensorType;
        System.Type _actuatorType;

        private int _sensorSize;
        private int _actuatorSize;


        private float[] sensorData = new float[0]; // Hopefully soon a NativeArray
        public NNDecision(NNModel model){
            _model = model;
            D.logEnabled = _verbose;
            _engine?.Dispose();
                
            _barracudaModel = ModelLoader.Load(model.Value);
            var executionDevice = inferenceDevice == InferenceDevice.GPU
                ? BarracudaWorkerFactory.Type.ComputeFast
                : BarracudaWorkerFactory.Type.CSharpFast;
                                       
            _engine = BarracudaWorkerFactory.CreateWorker(
                executionDevice, _barracudaModel, _verbose);

        }

        public void BatchProcess(ref NativeArray<TS> sensors, ref NativeArray<TA> actuators )
        {
            VerifySensor();
            VerifyActuator();
            
            int batch = sensors.Length;
            if (batch != actuators.Length)
            {
                throw new Exception("Error in the length of the sensors and actuators");
            }






            // unsafe{
            //     fixed (float* s = &sensorData[0]) { 
            //         UnsafeUtility.MemCpy(s , sensors.GetUnsafePtr(), batch * _sensorSize);
            //     }
            // }

            var tmpS = new NativeArray<float>(batch * _sensorSize / SIZE_OF_FLOAT_IN_MEMORY, Allocator.Persistent);

            for(var i = 0; i< batch; i++){
                var ss = sensors[i];
                TensorUtility.CopyToNativeArray(ss, tmpS,  i * _sensorSize );

            //      unsafe
            // {
            //     UnsafeUtility.CopyStructureToPtr(ref ss, (byte*) (tmpS.GetUnsafePtr()) + i * _sensorSize);
            // }
            }


            sensorData = tmpS.ToArray();

            var _sensorT = new Tensor(
                new TensorShape(batch, _sensorSize/ SIZE_OF_FLOAT_IN_MEMORY),
                sensorData,
                "sensor");

            _engine.Execute(_sensorT);
            _sensorT.Dispose();
            var actuatorT = _engine.Fetch("actuator");

            // actuators.Slice(
            //     0, _actuatorSize*batch).CopyFrom(actuatorT.data.Download(actuators.Length));
            // unsafe{
            //     fixed (float*  a = & (actuatorT.data.Download(batch)[0])) { 
            //         UnsafeUtility.MemCpy(actuators.GetUnsafePtr(), a, batch * _actuatorSize);
            //     }
            // }

            

            var tmpA = new NativeArray<float>(batch * _actuatorSize / SIZE_OF_FLOAT_IN_MEMORY, Allocator.Persistent);



            tmpA.CopyFrom(actuatorT.data.Download(tmpA.Length));


            for(var i = 0; i< batch; i++){
                var act = new TA();
                TensorUtility.CopyFromNativeArray(tmpA, out act, i * _actuatorSize );
                actuators[i] = act;
            }


            tmpS.Dispose();
            tmpA.Dispose();
            
        }


        private void VerifySensor(){
            if (! typeof(TS).Equals(_sensorType)){
                TensorUtility.DebugCheckStructure(typeof(TS));
                _sensorSize = UnsafeUtility.SizeOf<TS>();
                _sensorType = typeof(TS);
            }
        }
        private void VerifyActuator(){
            if (! typeof(TA).Equals(_actuatorType)){
                TensorUtility.DebugCheckStructure(typeof(TA));
                _actuatorSize = UnsafeUtility.SizeOf<TA>();
                _actuatorType = typeof(TA);
            }
        }


    }
}