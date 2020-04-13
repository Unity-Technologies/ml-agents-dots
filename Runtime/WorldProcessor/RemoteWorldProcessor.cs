using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class RemoteWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld world;
        private SharedMemoryCom com;
        private string policyId;

        public bool IsConnected {get {return true;}}

        internal RemoteWorldProcessor(MLAgentsWorld world, string policyId, SharedMemoryCom com)
        {
            this.world = world;
            this.com = com;
            this.policyId = policyId;
        }

        public void ProcessWorld()
        {
            com.WriteWorld(policyId, world);
            com.SetUnityReady();
            com.WaitForPython();
            com.LoadWorld(policyId, world);
        }

        public void Dispose()
        {
        }
    }
}
