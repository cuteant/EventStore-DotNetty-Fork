namespace EventStore.ClientAPI
{
  public interface IStreamEventsSlice
  {
    /// <summary>The <see cref="SliceReadStatus"/> representing the status of this read attempt</summary>
    SliceReadStatus Status { get; }

    /// <summary>The name of the stream read</summary>
    string Stream { get; }

    /// <summary>The starting point (represented as a sequence number) of the read operation.</summary>
    long FromEventNumber { get; }

    /// <summary>The direction of read request.</summary>
    ReadDirection ReadDirection { get; }

    /// <summary>The next event number that can be read.</summary>
    long NextEventNumber { get; }

    /// <summary>The last event number in the stream.</summary>
    long LastEventNumber { get; }

    /// <summary>A boolean representing whether or not this is the end of the stream.</summary>
    bool IsEndOfStream { get; }
  }

  public interface IStreamEventsSlice<TResolvedEvent> : IStreamEventsSlice where TResolvedEvent : IResolvedEvent
  {
    /// <summary>The events read represented as <see cref="ResolvedEvent"/></summary>
    TResolvedEvent[] Events { get; }
  }
}
