using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    internal class NullWorldProcessor : IWorldProcessor
    {
        private MLAgentsWorld world;
        internal NullWorldProcessor(MLAgentsWorld world)
        {
            this.world = world;
        }

        public void ProcessWorld()
        {
            world.SetActionReady();
            world.ResetDecisionsCounter();
        }

        public void ResetWorld()
        {
            world.ResetActionsCounter();
            world.ResetDecisionsCounter();
        }


        public void Dispose()
        {

        }
    }

}
