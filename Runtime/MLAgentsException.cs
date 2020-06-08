using System;

namespace Unity.AI.MLAgents
{
    /// <summary>
    /// Contains exceptions specific to ML-Agents.
    /// </summary>
    public class MLAgentsException : Exception
    {
        /// <summary>
        /// This exception indicates an error occurred in the ML-Agents package
        /// </summary>
        /// <param name="message">Text message for the error</param>
        /// <returns></returns>
        public MLAgentsException(string message) : base(message) {}
    }
}
