using System;
using Unity.Collections;


namespace DOTS_MLAgents.Core
{

    public interface IWorldProcessor : IDisposable
    {
        // We separate World processing into 3 methods to allow batching.
        void WriteWorldData();
        void ProcessWorldData();
        void RetrieveWorldData();
    }

    public unsafe class BarracudaWorldProcessor : IWorldProcessor
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

        public BarracudaWorldProcessor(MLAgentsWorld world/* Barracuda model*/)
        {

        }

        public void WriteWorldData()
        {

            // var _sensorT = new Tensor(
            //     new TensorShape(world.AgentCounter.Count, world.SensorFloatSize),
            //     world.Sensors.ToArray(),
            //     "sensor");
        }
        public void ProcessWorldData()
        {
            // _engine.Execute(_sensorT);
            // _sensorT.Dispose();
            // var actuatorT = _engine.Fetch("actuator");
        }
        public void RetrieveWorldData()
        {
            // world.Actuators.Slice(0, world.AgentCounter.Count * world.SensorFloatSize).CopyFrom(actuatorT.data.Download(world.AgentCounter.Count * world.SensorFloatSize));
            // actuatorT.Dispose();
        }


        public void Dispose()
        {
            // _engine.Dispose();
        }
    }

    public class HeuristicWorldProcessor<T> : IWorldProcessor where T : struct
    {

        private Func<T> heuristic;
        private MLAgentsWorld world;
        private T action;
        public HeuristicWorldProcessor(MLAgentsWorld world, Func<T> heuristic)
        {
            this.world = world;
            this.heuristic = heuristic;
        }

        public void WriteWorldData()
        {

        }
        public void ProcessWorldData()
        {
            action = heuristic.Invoke();
        }
        public void RetrieveWorldData()
        {
            for (int i = 0; i < world.AgentCounter.Count; i++)
            {
                if (world.ActionType == ActionType.CONTINUOUS)
                {

                    var s = world.ContinuousActuators.Slice(0, world.AgentCounter.Count * world.ActionSize).SliceConvert<T>();
                    s[i] = action;
                }
                else
                {
                    var s = world.DiscreteActuators.Slice(0, world.AgentCounter.Count * world.ActionSize).SliceConvert<T>();
                    s[i] = action;
                }
            }
        }


        public void Dispose()
        {

        }
    }

    public class CommunicatorWorldProcessor<T> : IWorldProcessor where T : struct
    {
        private MLAgentsWorld world;
        SharedMemoryCom com;
        string name;
        public CommunicatorWorldProcessor(string name, MLAgentsWorld world, SharedMemoryCom com)
        {
            this.world = world;
            this.com = com;
            this.name = name;
        }

        public void WriteWorldData()
        {
            // TODO : Move some of the logic from the communicator to here for the first reset
            com.WriteWorld(name, world);
        }
        public void ProcessWorldData()
        {

        }
        public void RetrieveWorldData()
        {
            com.LoadWorld(name, world);
        }

        public void Dispose()
        {

        }
    }

}
