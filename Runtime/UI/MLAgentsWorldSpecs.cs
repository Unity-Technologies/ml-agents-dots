using Unity.Mathematics;
using Unity.Entities;
using Barracuda;
using System;

namespace Unity.AI.MLAgents
{
    [Serializable]
    public struct MLAgentsWorldSpecs
    {
        public string Name;

        public int NumberAgents;
        public ActionType ActionType;
        public int3[] ObservationShapes;
        public int ActionSize;
        public int[] DiscreteActionBranches;

        public NNModel Model;
        public InferenceDevice InferenceDevice;

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
