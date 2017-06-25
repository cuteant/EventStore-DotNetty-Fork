using System;

namespace EventStore.ClientAPI
{
  public interface IResolvedEvent<out T> : IResolvedEvent2 where T : class
  {
    IRecordedEvent<T> OriginalEvent { get; }

    T Body { get; }
  }

  public interface IResolvedEvent2 : IResolvedEvent
  {
    IEventDescriptor OriginalEventDescriptor { get; }

    IRecordedEvent GetOriginalEvent();

    object GetBody();
  }

  public interface IResolvedEvent
  {
    bool IsResolved { get; }

    Position? OriginalPosition { get; }

    string OriginalStreamId { get; }

    Guid OriginalEventId { get; }

    long OriginalEventNumber { get; }

    string OriginalEventType { get; }
  }
}
