using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class RemoteWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld m_World;
        private SharedMemoryCommunicator m_Communicator;
        private string m_PolicyId;

        public bool IsConnected {get {return true;}}

        internal RemoteWorldProcessor(MLAgentsWorld world, string policyId, SharedMemoryCommunicator com)
        {
            this.m_World = world;
            this.m_Communicator = com;
            this.m_PolicyId = policyId;
        }

        public void ProcessWorld()
        {
            m_Communicator.WriteWorld(m_PolicyId, m_World);
            m_Communicator.SetUnityReady();
            m_Communicator.WaitForPython();
            m_Communicator.LoadWorld(m_PolicyId, m_World);
        }

        public void Dispose()
        {
        }
    }
}
