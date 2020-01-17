using System;

namespace DOTS_MLAgents.Core
{
    /// Contains exceptions specific to ML-Agents.
    [Serializable]
    internal class MLAgentsException : Exception
    {

        public MLAgentsException(string message) : base(message)
        {
        }

        protected MLAgentsException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
        }
    }
}