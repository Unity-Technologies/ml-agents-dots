using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.AI.MLAgents
{
    public interface IWorldProcessor : IDisposable
    {
        bool IsConnected{get;}
        RemoteCommand ProcessWorld();
    }
}
