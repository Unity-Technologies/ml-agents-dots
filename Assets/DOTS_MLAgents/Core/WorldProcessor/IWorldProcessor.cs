using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace DOTS_MLAgents.Core
{

    public interface IWorldProcessor : IDisposable
    {
        void ProcessWorld();
        void ResetWorld();
    }


}
