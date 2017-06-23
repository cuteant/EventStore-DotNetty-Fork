using System;

namespace EventStore.ClientAPI
{
  public interface IResolvedEvent<out T> : IResolvedEvent2 where T : class
  {
    IRecordedEvent<T> OriginalEvent { get; }
  }

  public interface IResolvedEvent2 : IResolvedEvent
  {
    IRecordedEvent GetOriginalEvent();

    IEventDescriptor GetDescriptor();

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
