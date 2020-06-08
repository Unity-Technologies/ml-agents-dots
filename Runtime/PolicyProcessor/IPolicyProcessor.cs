using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    /// <summary>
    /// The interface for a Policy processor. A Policy processor updates the
    /// action data of a Policy using the observation data present in it.
    /// </summary>
    public interface IPolicyProcessor : IDisposable
    {
        /// <summary>
        /// True if the Policy Processor is connected to the Python process
        /// </summary>
        bool IsConnected {get;}

        /// <summary>
        /// This method is called once everytime the policy needs to update its action
        /// data. The implementation of this mehtod must give new action data to all the
        /// agents that requested a decision.
        /// </summary>
        void Process();
    }
}
