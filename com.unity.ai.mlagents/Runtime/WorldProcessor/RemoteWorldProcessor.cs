using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class RemoteWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld world;
        private SharedMemoryCom com;
        private NativeString64 policyId;

        public bool IsConnected{get{return true;}}

        internal RemoteWorldProcessor(MLAgentsWorld world, string policyId, SharedMemoryCom com)
        {
            this.world = world;
            this.com = com;
            this.policyId = policyId;
        }

        public RemoteCommand ProcessWorld()
        {
            com.WriteWorld(policyId, world);
            com.SetUnityReady();
            var command = com.Advance();
            com.LoadWorld(policyId, world);

            return command;
        }

        public void Dispose()
        {
        }
    }
}
