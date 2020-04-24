using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class NullWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld m_World;

        public bool IsConnected {get {return false;}}

        internal NullWorldProcessor(MLAgentsWorld world)
        {
            this.m_World = world;
        }

        public void ProcessWorld()
        {
        }

        public void Dispose()
        {
        }
    }
}
