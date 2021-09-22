using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Barracuda;

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

        System.Collections.Generic.List<float[]> obsArrays;
        float[] maskArrays;

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

            obsArrays = new System.Collections.Generic.List<float[]>();
            for (int i = 0; i < m_Policy.SensorShapes.Length; i++)
            {
                var obsSize = m_Policy.SensorShapes[i].GetTotalTensorSize();
                obsArrays.Add(new float[m_Policy.DecisionAgentIds.Length * obsSize]);
            }
            if (m_Policy.DiscreteActionBranches.Length > 0)
            {
                maskArrays = new float[m_Policy.DecisionAgentIds.Length * m_Policy.DiscreteActionBranches.Sum()];
            }
        }

        public void Process()
        {
            var input = new System.Collections.Generic.Dictionary<string, Tensor>();

            var sensorOffset = 0;
            for (int sensorIndex = 0; sensorIndex < m_Policy.SensorShapes.Length; sensorIndex++)
            {
                var shape = m_Policy.SensorShapes[sensorIndex];
                fixed(void* arrPtr = obsArrays[sensorIndex])
                {
                    UnsafeUtility.MemCpy(
                        (byte*)arrPtr,
                        (byte*)m_Policy.DecisionObs.GetUnsafePtr() + 4 * sensorOffset,
                        shape.GetTotalTensorSize() * 4 * m_Policy.DecisionCounter.Count
                    );
                }
                sensorOffset += m_Policy.DecisionAgentIds.Length * shape.GetTotalTensorSize();

                if (shape.GetDimensions() == 1)
                {
                    input[$"obs_{sensorIndex}"] = new Tensor(
                        new TensorShape(m_Policy.DecisionCounter.Count, shape.x),
                        obsArrays[sensorIndex],
                        $"obs_{sensorIndex}");
                }
                else
                {
                    input[$"obs_{sensorIndex}"] = new Tensor(
                        new TensorShape(m_Policy.DecisionCounter.Count, shape.x, shape.y, shape.z),
                        obsArrays[sensorIndex],
                        $"obs_{sensorIndex}");
                }
            }

            if (m_Policy.DiscreteActionBranches.Length > 0)
            {
                for (int i = 0; i < m_Policy.DiscreteActionBranches.Sum() * m_Policy.DecisionCounter.Count; i++)
                {
                    maskArrays[i] = m_Policy.DecisionActionMasks[i] ? 0f : 1f; // masks are inverted
                }

                input[$"action_masks"] = new Tensor(
                    new TensorShape(m_Policy.DecisionCounter.Count, m_Policy.DiscreteActionBranches.Sum()),
                    maskArrays,
                    $"action_masks");
            }


            m_Engine.Execute(input);

            // Continuous case
            if (m_Policy.ContinuousActionSize > 0)
            {
                var actuatorTC = m_Engine.CopyOutput("continuous_actions");
                int count = m_Policy.DecisionCounter.Count * m_Policy.ContinuousActionSize;
                var shapeTC = new TensorShape(m_Policy.DecisionCounter.Count, m_Policy.ContinuousActionSize);
                var wholeData = actuatorTC.data.Download(shapeTC);

                fixed(void* arrPtr = wholeData)
                {
                    UnsafeUtility.MemCpy(
                        m_Policy.ContinuousActuators.GetUnsafePtr(),
                        arrPtr,
                        count * 4
                    );
                }
                actuatorTC.Dispose();
            }
            // discrete case
            if (m_Policy.DiscreteActionBranches.Length > 0)
            {
                var actuatorTD = m_Engine.CopyOutput("discrete_actions");
                int count = m_Policy.DecisionCounter.Count * m_Policy.DiscreteActionBranches.Length;
                var shapeTD = new TensorShape(m_Policy.DecisionCounter.Count, m_Policy.DiscreteActionBranches.Length);
                var wholeData = actuatorTD.data.Download(shapeTD);

                for (int i = 0; i < count; i++)
                {
                    m_Policy.DiscreteActuators[i] = (int)wholeData[i];
                }

                actuatorTD.Dispose();
            }
        }

        public void Dispose()
        {
            m_Engine.Dispose();
        }
    }
}
