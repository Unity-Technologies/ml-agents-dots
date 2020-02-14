using Unity.Mathematics;
using Unity.Entities;
using Barracuda;

namespace Unity.AI.MLAgents{
    
    [System.Serializable]
    public struct MLAgentsWorldSpecs{
        public string Name;

        public int NumberAgents;
        public ActionType ActionType;
        public int3[] ObservationShapes;
        public int ActionSize;
        public int[] DiscreteActionBranches;

        public NNModel Model;
        public InferenceDevice InferenceDevice;

        public MLAgentsWorld GenerateWorld(){
            return new MLAgentsWorld(
                NumberAgents,
                ActionType,
                ObservationShapes,
                ActionSize,
                DiscreteActionBranches
                );
        }

        public MLAgentsWorld GenerateAndRegisterWorld(){
            var world = GenerateWorld();
            world.SubscribeWorldWithBarracudaModel(Name, Model, InferenceDevice);
            return world;
        }
    }

}
