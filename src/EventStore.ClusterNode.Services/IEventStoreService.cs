using System;

namespace EventStore.ClusterNode
{
    public interface IEventStoreService
    {
        bool Start();

        bool Stop();
    }
}
