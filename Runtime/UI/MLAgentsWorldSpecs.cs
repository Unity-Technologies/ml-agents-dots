using Unity.Mathematics;
using Unity.Entities;
using Barracuda;
using System;
using UnityEngine;

namespace Unity.AI.MLAgents
{
    internal enum WorldProcessorType
    {
        Default,
        InferenceOnly,
        None
    }

    /// <summary>
    /// An editor friendly constructor for a MLAgentsWorld.
    /// Keeps track of the behavior specs of a world, its name,
    /// its processor type and Neural Network Model.
    /// </summary>
    [Serializable]
    public struct MLAgentsWorldSpecs
    {
        [SerializeField] internal string Name;

        [SerializeField] internal WorldProcessorType WorldProcessorType;

        [SerializeField] internal int NumberAgents;
        [SerializeField] internal ActionType ActionType;
        [SerializeField] internal int3[] ObservationShapes;
        [SerializeField] internal int ActionSize;
        [SerializeField] internal int[] DiscreteActionBranches;

        [SerializeField] internal NNModel Model;
        [SerializeField] internal InferenceDevice InferenceDevice;

        private MLAgentsWorld m_World;

        /// <summary>
        /// Generates the world using the specified specs and registers its
        /// processor to the Academy. The world is only created and registed once,
        /// if subsequent calls are made, the created world will be returned.
        /// </summary>
        /// <returns></returns>
        public MLAgentsWorld GetWorld()
        {
            if (m_World.IsCreated)
            {
                return m_World;
            }
            m_World = new MLAgentsWorld(
                NumberAgents,
                ObservationShapes,
                ActionType,
                ActionSize,
                DiscreteActionBranches
            );
            switch (WorldProcessorType)
            {
                case WorldProcessorType.Default:
                    m_World.RegisterWorldWithBarracudaModel(Name, Model, InferenceDevice);
                    break;
                case WorldProcessorType.InferenceOnly:
                    if (Model == null)
                    {
                        throw new MLAgentsException($"No model specified for {Name}");
                    }
                    m_World.RegisterWorldWithBarracudaModelForceNoCommunication(Name, Model, InferenceDevice);
                    break;
                case WorldProcessorType.None:
                    Academy.Instance.RegisterWorld(Name, m_World, new NullWorldProcessor(m_World), false);
                    break;
                default:
                    throw new MLAgentsException($"Unknown WorldProcessor Type");
            }

            return m_World;
        }
    }
}
