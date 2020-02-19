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

        private bool m_AlreadyCreated;

        public MLAgentsWorld GenerateWorld()
        {
            if (m_AlreadyCreated){
                throw new MLAgentsException(
                    "World has already been generated using Specs"
                    );
            }
            m_AlreadyCreated = true;
            return new MLAgentsWorld(
                NumberAgents,
                ActionType,
                ObservationShapes,
                ActionSize,
                DiscreteActionBranches
            );
        }

        public MLAgentsWorld GenerateAndRegisterWorld()
        {
            var world = GenerateWorld();
            world.SubscribeWorldWithBarracudaModel(Name, Model, InferenceDevice);
            return world;
        }
    }
}
