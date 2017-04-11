using System;

namespace EventStore.ClusterNode
{
  public interface IEventStoreService
  {
    void Start();

    void Stop();
  }
}
