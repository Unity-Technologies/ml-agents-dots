using Unity.Mathematics;
using Unity.Entities;
using Barracuda;
using System;
using UnityEngine;

namespace Unity.AI.MLAgents
{
    [Serializable]
    public struct MLAgentsWorldSpecs
    {
        [SerializeField] internal string Name;

        [SerializeField] public int NumberAgents;
        [SerializeField] public ActionType ActionType;
        [SerializeField] public int3[] ObservationShapes;
        [SerializeField] public int ActionSize;
        [SerializeField] public int[] DiscreteActionBranches;

        [SerializeField] public NNModel Model;
        [SerializeField] public InferenceDevice InferenceDevice;

        private MLAgentsWorld m_World;

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
            m_World.RegisterWorldWithBarracudaModel(Name, Model, InferenceDevice);
            return m_World;
        }
    }
}
