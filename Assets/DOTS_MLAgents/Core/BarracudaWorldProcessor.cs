using System;


namespace DOTS_MLAgents.Core
{
    public unsafe class BarracudaWorldProcessor : IDisposable
    {

        // private NNModel _model;
        // public InferenceDevice inferenceDevice = InferenceDevice.CPU;
        // private Model _barracudaModel;
        // private IWorker _engine;
        // private const bool _verbose = false;

        // public BarracudaWorldProcessor(NNModel model)
        // {
        // _model = model;
        // D.logEnabled = _verbose;
        // _engine?.Dispose();

        // // _barracudaModel = ModelLoader.Load(model.Value);
        // var executionDevice = inferenceDevice == InferenceDevice.GPU
        //     ? BarracudaWorkerFactory.Type.ComputeFast
        //     : BarracudaWorkerFactory.Type.CSharpFast;

        // _engine = BarracudaWorkerFactory.CreateWorker(
        //     executionDevice, _barracudaModel, _verbose);
        // }

        public void ProcessWorld(MLAgentsWorld world)
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
        }


        public void Dispose()
        {
            // _engine.Dispose();
        }
    }
}
