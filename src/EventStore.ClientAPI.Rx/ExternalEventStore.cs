using System;

namespace EventStore.ClientAPI.Rx
{
  public class ExternalEventStore : IEventStore
  {
    public ExternalEventStore(Uri uri)
    {
      Connection = EventStoreConnection.Create(EventStoreConnectionSettings.Default, uri);
    }

    public IEventStoreConnection Connection { get; }
  }
}