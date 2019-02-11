using UnityEngine;

namespace ECS_MLAgents_v0.Core.Inference
{
    public class NNModel : ScriptableObject
    {
        [HideInInspector]
        public byte[] Value;
    }
}