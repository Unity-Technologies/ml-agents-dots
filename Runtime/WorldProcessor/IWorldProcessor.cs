using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    /// <summary>
    /// The interface for a world processor. A world processor updates the
    /// action data of a world using the observation data present in it.
    /// </summary>
    public interface IWorldProcessor : IDisposable
    {
        /// <summary>
        /// True if the World Processor is connected to the Python process
        /// </summary>
        bool IsConnected {get;}

        /// <summary>
        /// This method is called once everytime the world needs to update its action
        /// data. The implementation of this mehtod must give new action data to all the
        /// agents that requested a decision.
        /// </summary>
        void ProcessWorld();
    }
}
