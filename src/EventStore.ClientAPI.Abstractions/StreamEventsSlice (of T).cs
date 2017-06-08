using System;

namespace EventStore.ClientAPI
{
  /// <summary>An Stream Events Slice represents the result of a single read operation to the event store.</summary>
  public class StreamEventsSlice<T> where T : class
  {
    /// <summary>The <see cref="SliceReadStatus"/> representing the status of this read attempt</summary>
    public readonly SliceReadStatus Status;

    /// <summary>The name of the stream read</summary>
    public readonly string Stream;

    /// <summary>The starting point (represented as a sequence number) of the read operation.</summary>
    public readonly long FromEventNumber;

    /// <summary>The direction of read request.</summary>
    public readonly ReadDirection ReadDirection;

    /// <summary>The events read represented as <see cref="ResolvedEvent&lt;T&gt;"/></summary>
    public readonly ResolvedEvent<T>[] Events;

    /// <summary>The next event number that can be read.</summary>
    public readonly long NextEventNumber;

    /// <summary>The last event number in the stream.</summary>
    public readonly long LastEventNumber;

    /// <summary>A boolean representing whether or not this is the end of the stream.</summary>
    public readonly bool IsEndOfStream;

    internal StreamEventsSlice(SliceReadStatus status,
                                 string stream,
                                 long fromEventNumber,
                                 ReadDirection readDirection,
                                 ResolvedEvent<T>[] events,
                                 long nextEventNumber,
                                 long lastEventNumber,
                                 bool isEndOfStream)
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }

      Status = status;
      Stream = stream;
      FromEventNumber = fromEventNumber;
      ReadDirection = readDirection;
      Events = events;
      NextEventNumber = nextEventNumber;
      LastEventNumber = lastEventNumber;
      IsEndOfStream = isEndOfStream;
    }
  }
}