using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Barracuda;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Where to perform inference.
    /// </summary>
    public enum InferenceDevice
    {
        /// <summary>
        /// CPU inference
        /// </summary>
        CPU = 0,

        /// <summary>
        /// GPU inference
        /// </summary>
        GPU = 1
    }

    public static class BarracudaWorldProcessorRegistringExtension
    {
        /// <summary>
        /// Registers the given MLAgentsWorld to the Academy with a Neural
        /// Network Model. If the input model is null, a default inactive
        /// processor will be registered instead. Note that if the simulation
        /// connects to Python, the Neural Network will be ignored and the world
        /// will exchange data with Python instead.
        /// </summary>
        /// <param name="world"> The MLAgentsWorld to register</param>
        /// <param name="policyId"> The name of the world. This is useful for identification
        /// and for training.</param>
        /// <param name="model"> The Neural Network model used by the processor</param>
        /// <param name="inferenceDevice"> The inference device specifying where to run inference
        /// (CPU or GPU)</param>
        public static void RegisterWorldWithBarracudaModel(
            this MLAgentsWorld world,
            string policyId,
            NNModel model,
            InferenceDevice inferenceDevice = InferenceDevice.CPU
        )
        {
            if (model != null)
            {
                var worldProcessor = new BarracudaWorldProcessor(world, model, inferenceDevice);
                Academy.Instance.RegisterWorld(policyId, world, worldProcessor, true);
            }
            else
            {
                Academy.Instance.RegisterWorld(policyId, world, null, true);
            }
        }

        /// <summary>
        /// Registers the given MLAgentsWorld to the Academy with a Neural
        /// Network Model. If the input model is null, a default inactive
        /// processor will be registered instead. Note that if the simulation
        /// connects to Python, the world will not connect to Python and run the
        /// given Neural Network regardless.
        /// </summary>
        /// <param name="world"> The MLAgentsWorld to register</param>
        /// <param name="policyId"> The name of the world. This is useful for identification
        /// and for training.</param>
        /// <param name="model"> The Neural Network model used by the processor</param>
        /// <param name="inferenceDevice"> The inference device specifying where to run inference
        /// (CPU or GPU)</param>
        public static void RegisterWorldWithBarracudaModelForceNoCommunication(
            this MLAgentsWorld world,
            string policyId,
            NNModel model,
            InferenceDevice inferenceDevice = InferenceDevice.CPU
        )
        {
            if (model != null)
            {
                var worldProcessor = new BarracudaWorldProcessor(world, model, inferenceDevice);
                Academy.Instance.RegisterWorld(policyId, world, worldProcessor, false);
            }
            else
            {
                Academy.Instance.RegisterWorld(policyId, world, null, false);
            }
        }
    }

    internal unsafe class BarracudaWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld m_World;
        private Model m_BarracudaModel;
        private IWorker m_Engine;
        private const bool k_Verbose = false;

        public bool IsConnected {get {return false;}}

        internal BarracudaWorldProcessor(MLAgentsWorld world, NNModel model, InferenceDevice inferenceDevice)
        {
            this.m_World = world;
            D.logEnabled = k_Verbose;
            m_Engine?.Dispose();

            m_BarracudaModel = ModelLoader.Load(model);
            var executionDevice = inferenceDevice == InferenceDevice.GPU
                ? WorkerFactory.Type.ComputePrecompiled
                : WorkerFactory.Type.CSharp;

            m_Engine = WorkerFactory.CreateWorker(
                executionDevice, m_BarracudaModel, k_Verbose);
        }

        public void ProcessWorld()
        {
            // TODO : Cover all cases
            // FOR VECTOR OBS ONLY
            // For Continuous control only
            // No LSTM
            int obsSize = 0;
            for (int i = 0; i < m_World.SensorShapes.Length; i++)
            {
                if (m_World.SensorShapes[i].GetDimensions() == 1)
                    obsSize += m_World.SensorShapes[i].GetTotalTensorSize();
            }

            var input = new System.Collections.Generic.Dictionary<string, Tensor>();

            var vectorObsArr = new float[m_World.DecisionCounter.Count * obsSize];
            var sensorData = m_World.DecisionObs.ToArray();
            int sensorOffset = 0;
            int vecObsOffset = 0;
            foreach (var shape in m_World.SensorShapes)
            {
                if (shape.GetDimensions() == 1)
                {
                    for (int i = 0; i < m_World.DecisionCounter.Count; i++)
                    {
                        Array.Copy(sensorData, sensorOffset + i * shape.GetTotalTensorSize(), vectorObsArr, i * obsSize + vecObsOffset, shape.GetTotalTensorSize());
                    }
                    sensorOffset += m_World.DecisionAgentIds.Length * shape.GetTotalTensorSize();
                    vecObsOffset += shape.GetTotalTensorSize();
                }
                else
                {
                    throw new MLAgentsException("TODO : Inference only works for continuous control and vector obs");
                }
            }

            input["vector_observation"] = new Tensor(
                new TensorShape(m_World.DecisionCounter.Count, obsSize),
                vectorObsArr,
                "vector_observation");

            m_Engine.ExecuteAndWaitForCompletion(input);

            var actuatorT = m_Engine.CopyOutput("action");

            switch (m_World.ActionType)
            {
                case ActionType.CONTINUOUS:
                    int count = m_World.DecisionCounter.Count * m_World.ActionSize;
                    var wholeData = actuatorT.data.Download(count);
                    var dest = new float[count];
                    Array.Copy(wholeData, dest, count);
                    m_World.ContinuousActuators.Slice(0, count).CopyFrom(dest);
                    break;
                case ActionType.DISCRETE:
                    throw new MLAgentsException("TODO : Inference only works for continuous control and vector obs");
                default:
                    break;
            }
            actuatorT.Dispose();
        }

        public void Dispose()
        {
            m_Engine.Dispose();
        }
    }
}
