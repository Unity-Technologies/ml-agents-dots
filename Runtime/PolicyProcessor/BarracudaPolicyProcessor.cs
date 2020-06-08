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

    public static class BarracudaPolicyProcessorRegistringExtension
    {
        /// <summary>
        /// Registers the given Policy to the Academy with a Neural
        /// Network Model. If the input model is null, a default inactive
        /// processor will be registered instead. Note that if the simulation
        /// connects to Python, the Neural Network will be ignored and the Policy
        /// will exchange data with Python instead.
        /// </summary>
        /// <param name="policy"> The Policy to register</param>
        /// <param name="policyId"> The name of the Policy. This is useful for identification
        /// and for training.</param>
        /// <param name="model"> The Neural Network model used by the processor</param>
        /// <param name="inferenceDevice"> The inference device specifying where to run inference
        /// (CPU or GPU)</param>
        public static void RegisterPolicyWithBarracudaModel(
            this Policy policy,
            string policyId,
            NNModel model,
            InferenceDevice inferenceDevice = InferenceDevice.CPU
        )
        {
            if (model != null)
            {
                var policyProcessor = new BarracudaPolicyProcessor(policy, model, inferenceDevice);
                Academy.Instance.RegisterPolicy(policyId, policy, policyProcessor, true);
            }
            else
            {
                Academy.Instance.RegisterPolicy(policyId, policy, null, true);
            }
        }

        /// <summary>
        /// Registers the given Policy to the Academy with a Neural
        /// Network Model. If the input model is null, a default inactive
        /// processor will be registered instead. Note that if the simulation
        /// connects to Python, the Policy will not connect to Python and run the
        /// given Neural Network regardless.
        /// </summary>
        /// <param name="policy"> The Policy to register</param>
        /// <param name="policyId"> The name of the Policy. This is useful for identification
        /// and for training.</param>
        /// <param name="model"> The Neural Network model used by the processor</param>
        /// <param name="inferenceDevice"> The inference device specifying where to run inference
        /// (CPU or GPU)</param>
        public static void RegisterPolicyWithBarracudaModelForceNoCommunication(
            this Policy policy,
            string policyId,
            NNModel model,
            InferenceDevice inferenceDevice = InferenceDevice.CPU
        )
        {
            if (model != null)
            {
                var policyProcessor = new BarracudaPolicyProcessor(policy, model, inferenceDevice);
                Academy.Instance.RegisterPolicy(policyId, policy, policyProcessor, false);
            }
            else
            {
                Academy.Instance.RegisterPolicy(policyId, policy, null, false);
            }
        }
    }

    internal unsafe class BarracudaPolicyProcessor : IPolicyProcessor
    {
        private Policy m_Policy;
        private Model m_BarracudaModel;
        private IWorker m_Engine;
        private const bool k_Verbose = false;

        int obsSize;
        float[] vectorObsArr;

        public bool IsConnected {get {return false;}}

        internal BarracudaPolicyProcessor(Policy policy, NNModel model, InferenceDevice inferenceDevice)
        {
            this.m_Policy = policy;
            D.logEnabled = k_Verbose;
            m_Engine?.Dispose();

            m_BarracudaModel = ModelLoader.Load(model);
            var executionDevice = inferenceDevice == InferenceDevice.GPU
                ? WorkerFactory.Type.ComputePrecompiled
                : WorkerFactory.Type.CSharp;

            m_Engine = WorkerFactory.CreateWorker(
                executionDevice, m_BarracudaModel, k_Verbose);
            for (int i = 0; i < m_Policy.SensorShapes.Length; i++)
            {
                if (m_Policy.SensorShapes[i].GetDimensions() == 1)
                    obsSize += m_Policy.SensorShapes[i].GetTotalTensorSize();
            }
            vectorObsArr = new float[m_Policy.DecisionAgentIds.Length * obsSize];
        }

        public void Process()
        {
            // TODO : Cover all cases
            // FOR VECTOR OBS ONLY
            // For Continuous control only
            // No LSTM

            var input = new System.Collections.Generic.Dictionary<string, Tensor>();

            // var sensorData = m_Policy.DecisionObs.ToArray();
            int sensorOffset = 0;
            int vecObsOffset = 0;
            foreach (var shape in m_Policy.SensorShapes)
            {
                if (shape.GetDimensions() == 1)
                {
                    for (int i = 0; i < m_Policy.DecisionCounter.Count; i++)
                    {
                        fixed(void* arrPtr = vectorObsArr)
                        {
                            UnsafeUtility.MemCpy(
                                (byte*)arrPtr + 4 * i * obsSize + 4 * vecObsOffset,
                                (byte*)m_Policy.DecisionObs.GetUnsafePtr() + 4 * sensorOffset + 4 * i * shape.GetTotalTensorSize(),
                                shape.GetTotalTensorSize() * 4
                            );
                        }

                        // Array.Copy(sensorData, sensorOffset + i * shape.GetTotalTensorSize(), vectorObsArr, i * obsSize + vecObsOffset, shape.GetTotalTensorSize());
                    }
                    sensorOffset += m_Policy.DecisionAgentIds.Length * shape.GetTotalTensorSize();
                    vecObsOffset += shape.GetTotalTensorSize();
                }
                else
                {
                    throw new MLAgentsException("TODO : Inference only works for continuous control and vector obs");
                }
            }

            input["vector_observation"] = new Tensor(
                new TensorShape(m_Policy.DecisionCounter.Count, obsSize),
                vectorObsArr,
                "vector_observation");

            m_Engine.ExecuteAndWaitForCompletion(input);

            var actuatorT = m_Engine.CopyOutput("action");

            switch (m_Policy.ActionType)
            {
                case ActionType.CONTINUOUS:
                    int count = m_Policy.DecisionCounter.Count * m_Policy.ActionSize;
                    var wholeData = actuatorT.data.Download(count);
                    // var dest = new float[count];
                    // Array.Copy(wholeData, dest, count);
                    // m_Policy.ContinuousActuators.Slice(0, count).CopyFrom(dest);
                    fixed(void* arrPtr = wholeData)
                    {
                        UnsafeUtility.MemCpy(
                            m_Policy.ContinuousActuators.GetUnsafePtr(),
                            arrPtr,
                            count * 4
                        );
                    }
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
