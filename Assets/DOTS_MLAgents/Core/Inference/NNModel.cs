using UnityEngine;

namespace DOTS_MLAgents.Core.Inference
{
    public class NNModel : ScriptableObject
    {
        [HideInInspector]
        public byte[] Value;
    }
}