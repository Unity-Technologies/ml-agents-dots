using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace DOTS_MLAgents.Core
{

    internal unsafe class BarracudaWorldProcessor : IWorldProcessor
    {
        MLAgentsWorld world;
        // private NNModel _model;
        // public InferenceDevice inferenceDevice = InferenceDevice.CPU;
        // private Model _barracudaModel;
        // private IWorker _engine;
        // private const bool _verbose = false;

        internal BarracudaWorldProcessor(MLAgentsWorld world/* Barracuda model*/)
        {
            this.world = world;
            // _model = model;
            // D.logEnabled = _verbose;
            // _engine?.Dispose();

            // // _barracudaModel = ModelLoader.Load(model.Value);
            // var executionDevice = inferenceDevice == InferenceDevice.GPU
            //     ? BarracudaWorkerFactory.Type.ComputeFast
            //     : BarracudaWorkerFactory.Type.CSharpFast;

            // _engine = BarracudaWorkerFactory.CreateWorker(
            //     executionDevice, _barracudaModel, _verbose);
        }


        public void ProcessWorld()
        {
            // var _sensorT = new Tensor(
            //     new TensorShape(world.AgentCounter.Count, world.SensorFloatSize),
            //     world.Sensors.ToArray(),
            //     "sensor");
            // _engine.Execute(_sensorT);
            // _sensorT.Dispose();
            // var actuatorT = _engine.Fetch("actuator");
            // world.Actuators.Slice(0, world.AgentCounter.Count * world.SensorFloatSize).CopyFrom(actuatorT.data.Download(world.AgentCounter.Count * world.SensorFloatSize));
            // actuatorT.Dispose();
            world.SetActionReady();
            world.ResetDecisionsCounter();
        }

        public void ResetWorld()
        {
            world.ResetActionsCounter();
            world.ResetDecisionsCounter();
        }



        public void Dispose()
        {
            // _engine.Dispose();
        }
    }

}
