using System;

namespace EventStore.ClientAPI
{
  public interface IRecordedEvent<out T>: IRecordedEvent where T : class
  {
    /// <summary>The full event info.</summary>
    IFullEvent<T> FullEvent { get; }
  }

  public interface IRecordedEvent
  {
    /// <summary>The Event Stream that this event belongs to</summary>
    string EventStreamId { get; }

    /// <summary>The Unique Identifier representing this event</summary>
    Guid EventId { get; }

    /// <summary>The number of this event in the stream</summary>
    long EventNumber { get; }

    /// <summary>The type of event this is</summary>
    string EventType { get; }

    /// <summary>Indicates whether the content is internally marked as json</summary>
    bool IsJson { get; }

    /// <summary>A datetime representing when this event was created in the system</summary>
    DateTime Created { get; }

    /// <summary>A long representing the milliseconds since the epoch when the was created in the system</summary>
    long CreatedEpoch { get; }
  }
}
