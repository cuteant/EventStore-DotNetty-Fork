using System;

namespace EventStore.ClientAPI
{
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
