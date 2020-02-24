using System;

namespace Unity.AI.MLAgents
{
    /// Contains exceptions specific to ML-Agents.
    internal class MLAgentsException : Exception
    {
        public MLAgentsException(string message) : base(message)
        {
        }
    }
}
