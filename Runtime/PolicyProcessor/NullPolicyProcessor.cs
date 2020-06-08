using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class NullPolicyProcessor : IPolicyProcessor
    {
        private Policy m_Policy;

        public bool IsConnected {get {return false;}}

        internal NullPolicyProcessor(Policy policy)
        {
            this.m_Policy = policy;
        }

        public void Process()
        {
        }

        public void Dispose()
        {
        }
    }
}
