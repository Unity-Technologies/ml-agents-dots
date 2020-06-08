using Unity.Mathematics;
using Unity.Entities;
using Barracuda;
using System;
using UnityEngine;

namespace Unity.AI.MLAgents
{
    internal enum PolicyProcessorType
    {
        Default,
        InferenceOnly,
        None
    }

    /// <summary>
    /// An editor friendly constructor for a Policy.
    /// Keeps track of the behavior specs of a Policy, its name,
    /// its processor type and Neural Network Model.
    /// </summary>
    [Serializable]
    public struct PolicySpecs
    {
        [SerializeField] internal string Name;

        [SerializeField] internal PolicyProcessorType PolicyProcessorType;

        [SerializeField] internal int NumberAgents;
        [SerializeField] internal ActionType ActionType;
        [SerializeField] internal int3[] ObservationShapes;
        [SerializeField] internal int ActionSize;
        [SerializeField] internal int[] DiscreteActionBranches;

        [SerializeField] internal NNModel Model;
        [SerializeField] internal InferenceDevice InferenceDevice;

        private Policy m_Policy;

        /// <summary>
        /// Generates a Policy using the specified specs and registers its
        /// processor to the Academy. The policy is only created and registed once,
        /// if subsequent calls are made, the created policy will be returned.
        /// </summary>
        /// <returns></returns>
        public Policy GetPolicy()
        {
            if (m_Policy.IsCreated)
            {
                return m_Policy;
            }
            m_Policy = new Policy(
                NumberAgents,
                ObservationShapes,
                ActionType,
                ActionSize,
                DiscreteActionBranches
            );
            switch (PolicyProcessorType)
            {
                case PolicyProcessorType.Default:
                    m_Policy.RegisterPolicyWithBarracudaModel(Name, Model, InferenceDevice);
                    break;
                case PolicyProcessorType.InferenceOnly:
                    if (Model == null)
                    {
                        throw new MLAgentsException($"No model specified for {Name}");
                    }
                    m_Policy.RegisterPolicyWithBarracudaModelForceNoCommunication(Name, Model, InferenceDevice);
                    break;
                case PolicyProcessorType.None:
                    Academy.Instance.RegisterPolicy(Name, m_Policy, new NullPolicyProcessor(m_Policy), false);
                    break;
                default:
                    throw new MLAgentsException($"Unknown IPolicyProcessor Type");
            }

            return m_Policy;
        }
    }
}
