using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class NullWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld world;

        public bool IsConnected {get {return false;}}

        internal NullWorldProcessor(MLAgentsWorld world)
        {
            this.world = world;
        }

        public WorldCommand ProcessWorld()
        {
            return WorldCommand.DEFAULT;
        }

        public void Dispose()
        {
        }
    }
}
