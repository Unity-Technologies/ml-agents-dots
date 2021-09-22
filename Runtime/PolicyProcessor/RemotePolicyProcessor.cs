using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.AI.MLAgents.SideChannels;


namespace Unity.AI.MLAgents
{
    internal class RemotePolicyProcessor : IPolicyProcessor
    {
        private Policy m_Policy;
        private SharedMemoryCommunicator m_Communicator;
        private string m_PolicyId;

        public bool IsConnected {get {return true;}}

        internal RemotePolicyProcessor(Policy policy, string policyId, SharedMemoryCommunicator com)
        {
            this.m_Policy = policy;
            this.m_Communicator = com;
            this.m_PolicyId = policyId;
        }

        public void Process()
        {
            m_Communicator.WritePolicy(m_PolicyId, m_Policy);
            m_Communicator.SetUnityReady();
            m_Communicator.WaitForPython();
            AnswerQuery();
            m_Communicator.LoadPolicy(m_PolicyId, m_Policy);
        }

        public void Dispose()
        {
        }

        private void AnswerQuery()
        {
            while (m_Communicator.ReadAndClearQueryCommand())
            {
                SideChannelManager.ProcessSideChannelData(m_Communicator.ReadAndClearSideChannelData());
                m_Communicator.WriteSideChannelData(SideChannelManager.GetSideChannelMessage());
                m_Communicator.SetUnityReady();
                m_Communicator.WaitForPython();
            }
        }
    }
}
