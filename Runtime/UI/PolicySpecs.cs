using Unity.Mathematics;
using Unity.Entities;
using Unity.Barracuda;
using System;
using UnityEngine;

namespace Unity.AI.MLAgents
{
    public enum PolicyProcessorType
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
        #pragma warning disable CS0649
        [SerializeField] private string m_Name;

        [SerializeField] private PolicyProcessorType m_PolicyProcessorType;

        [SerializeField] private int m_NumberAgents;
        [SerializeField] private int3[] m_ObservationShapes;
        [SerializeField] private int m_ContinuousActionSize;
        [SerializeField] private int m_DiscreteActionSize;
        [SerializeField] private int[] m_DiscreteActionBranches;

        [SerializeField] private NNModel m_Model;
        [SerializeField] private InferenceDevice m_InferenceDevice;
#pragma warning restore CS0649

        public string Name
        {
            get { return m_Name; }
            set
            {
                AssertSpecIsEditable("Name");
                m_Name = value;
            }
        }
        public PolicyProcessorType PolicyProcessorType
        {
            get { return m_PolicyProcessorType; }
            set
            {
                AssertSpecIsEditable("PolicyProcessorType");
                m_PolicyProcessorType = value;
            }
        }


        public int NumberAgents
        {
            get { return m_NumberAgents; }
            set
            {
                AssertSpecIsEditable("NumberAgents");
                m_NumberAgents = value;
            }
        }


        public int3[] ObservationShapes
        {
            get { return m_ObservationShapes; }
            set
            {
                AssertSpecIsEditable("ObservationShapes");
                m_ObservationShapes = value;
            }
        }


        public int ContinuousActionSize
        {
            get { return m_ContinuousActionSize; }
            set
            {
                AssertSpecIsEditable("ContinuousActionSize");
                m_ContinuousActionSize = value;
            }
        }


        public int DiscreteActionSize
        {
            get { return m_DiscreteActionSize; }
            set
            {
                AssertSpecIsEditable("DiscreteActionSize");
                m_DiscreteActionSize = value;
            }
        }


        public int[] DiscreteActionBranches
        {
            get { return m_DiscreteActionBranches; }
            set
            {
                AssertSpecIsEditable("DiscreteActionBranches");
                m_DiscreteActionBranches = value;
            }
        }


        public NNModel Model
        {
            get { return m_Model; }
            set
            {
                AssertSpecIsEditable("Model");
                m_Model = value;
            }
        }


        public InferenceDevice InferenceDevice
        {
            get { return m_InferenceDevice; }
            set
            {
                AssertSpecIsEditable("InferenceDevice");
                m_InferenceDevice = value;
            }
        }


        private Policy m_Policy;

        private void AssertSpecIsEditable(string specName)
        {
            if (m_Policy.IsCreated)
            {
                throw new MLAgentsException($"Cannot edit PolicySpec {specName} after the policy is created. ");
            }
        }

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
                ContinuousActionSize,
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
