using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace DOTS_MLAgents.Core
{

    public interface IWorldProcessor : IDisposable
    {
        void ProcessWorld();
        void ResetWorld();
    }

    public unsafe class BarracudaWorldProcessor : IWorldProcessor
    {
        MLAgentsWorld world;
        // private NNModel _model;
        // public InferenceDevice inferenceDevice = InferenceDevice.CPU;
        // private Model _barracudaModel;
        // private IWorker _engine;
        // private const bool _verbose = false;

        public BarracudaWorldProcessor(MLAgentsWorld world/* Barracuda model*/)
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

    public class HeuristicWorldProcessor<T> : IWorldProcessor where T : struct
    {

        private Func<T> heuristic;
        private MLAgentsWorld world;
        public HeuristicWorldProcessor(MLAgentsWorld world, Func<T> heuristic)
        {
            this.world = world;
            this.heuristic = heuristic;
            var structSize = UnsafeUtility.SizeOf<T>() / sizeof(float);
            if (structSize != world.ActionSize)
            {
                throw new MLAgentsException(string.Format(
                    "The heuristic provided does not match the action size. Expected {0} received {1}", structSize, world.ActionSize));
            }
        }

        public void ProcessWorld()
        {
            T action = heuristic.Invoke();
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

        }
    }

}
