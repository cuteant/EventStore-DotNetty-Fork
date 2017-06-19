using System;

namespace EventStore.ClientAPI
{
  /// <summary>An Stream Events Slice represents the result of a single read operation to the event store.</summary>
  public class StreamEventsSlice : IStreamEventsSlice<ResolvedEvent>
  {
    /// <summary>The <see cref="SliceReadStatus"/> representing the status of this read attempt</summary>
    public SliceReadStatus Status { get; }

    /// <summary>The name of the stream read</summary>
    public string Stream { get; }

    /// <summary>The starting point (represented as a sequence number) of the read operation.</summary>
    public long FromEventNumber { get; }

    /// <summary>The direction of read request.</summary>
    public ReadDirection ReadDirection { get; }

    /// <summary>The events read represented as <see cref="ResolvedEvent"/></summary>
    public ResolvedEvent[] Events { get; }

    /// <summary>The next event number that can be read.</summary>
    public long NextEventNumber { get; }

    /// <summary>The last event number in the stream.</summary>
    public long LastEventNumber { get; }

    /// <summary>A boolean representing whether or not this is the end of the stream.</summary>
    public bool IsEndOfStream { get; }

    internal StreamEventsSlice(SliceReadStatus status,
                                 string stream,
                                 long fromEventNumber,
                                 ReadDirection readDirection,
                                 ResolvedEvent[] events,
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