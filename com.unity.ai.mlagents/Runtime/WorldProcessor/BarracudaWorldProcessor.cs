using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Barracuda;


namespace Unity.AI.MLAgents
{
    public enum InferenceDevice
    {
        CPU,
        GPU
    }

    public static class BarracudaWorldProcessorRegistringExtension
    {
        public static void SubscribeWorldWithBarracudaModel(
            this MLAgentsSystem system,
            string policyId,
            MLAgentsWorld world,
            NNModel model,
            InferenceDevice inferenceDevice = InferenceDevice.CPU
        )
        {
            if (model != null)
            {
                var worldProcessor = new BarracudaWorldProcessor(world, model, inferenceDevice);
                system.SubscribeWorld(policyId, world, worldProcessor, true);
            }
            else
            {
                system.SubscribeWorld(policyId, world, null, true);
            }
        }

        public static void SubscribeWorldWithBarracudaModelForceNoCommunication<TH>(
            this MLAgentsSystem system,
            string policyId,
            MLAgentsWorld world,
            NNModel model,
            InferenceDevice inferenceDevice = InferenceDevice.CPU
        )
        {
            var worldProcessor = new BarracudaWorldProcessor(world, model, inferenceDevice);
            system.SubscribeWorld(policyId, world, worldProcessor, false);
        }
    }
    internal unsafe class BarracudaWorldProcessor : IWorldProcessor
    {
        MLAgentsWorld world;
        private NNModel _model;
        public InferenceDevice inferenceDevice;
        private Model _barracudaModel;
        private IWorker _engine;
        private const bool _verbose = false;

        internal BarracudaWorldProcessor(MLAgentsWorld world, NNModel model, InferenceDevice inferenceDevice)
        {
            this.world = world;
            _model = model;
            D.logEnabled = _verbose;
            _engine?.Dispose();

            _barracudaModel = ModelLoader.Load(model);
            var executionDevice = inferenceDevice == InferenceDevice.GPU
                ? WorkerFactory.Type.ComputePrecompiled
                : WorkerFactory.Type.CSharp;

            _engine = BarracudaWorkerFactory.CreateWorker(
                executionDevice, _barracudaModel, _verbose);
        }

        public void ProcessWorld()
        {
            // FOR VECTOR OBS ONLY
            // For Continuois control only
            // No LSTM
            int obsSize = 0;
            for (int i = 0; i < world.SensorShapes.Length; i++)
            {
                if (world.SensorShapes[i].GetDimensions() == 1)
                    obsSize += world.SensorShapes[i].GetTotalTensorSize();
            }

            var input = new System.Collections.Generic.Dictionary<string, Tensor>();

            var vectorObsArr = new float[world.AgentCounter.Count * obsSize];
            var sensorData = world.Sensors.ToArray();
            int sensorOffset = 0;
            int vecObsOffset = 0;
            foreach (var shape in world.SensorShapes)
            {
                if (shape.GetDimensions() == 1)
                {
                    for (int i = 0; i < world.AgentCounter.Count; i++)
                    {
                        Array.Copy(sensorData, sensorOffset + i * shape.GetTotalTensorSize(), vectorObsArr, i * obsSize + vecObsOffset, shape.GetTotalTensorSize());
                    }
                    sensorOffset += world.AgentIds.Length * shape.GetTotalTensorSize();
                    vecObsOffset += shape.GetTotalTensorSize();
                }
            }

            input["vector_observation"] = new Tensor(
                new TensorShape(world.AgentCounter.Count, obsSize),
                vectorObsArr,
                "vector_observation");

            input["epsilon"] = new Tensor(
                new TensorShape(world.AgentCounter.Count, world.ActionSize),
                new float[world.AgentCounter.Count * world.ActionSize],
                "epsilon");

            _engine.ExecuteAndWaitForCompletion(input);

            var actuatorT = _engine.Fetch("action");

            switch (world.ActionType)
            {
                case ActionType.CONTINUOUS:
                    int count = world.AgentCounter.Count * world.ActionSize;
                    var wholeData = actuatorT.data.Download(count);
                    var dest = new float[count];
                    Array.Copy(wholeData, dest, count);
                    world.ContinuousActuators.Slice(0, count).CopyFrom(dest);
                    break;
                case ActionType.DISCRETE:
                    // The data copied was float data
                    int[] actionData = Array.ConvertAll(actuatorT.data.Download(world.AgentCounter.Count * world.ActionSize), x => (int)x);
                    world.DiscreteActuators.Slice(0, world.AgentCounter.Count * world.ActionSize).CopyFrom(actionData);
                    break;
                default:
                    break;
            }
            actuatorT.Dispose();


            // Mark the world as done processing
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
            _engine.Dispose();
        }
    }
}
