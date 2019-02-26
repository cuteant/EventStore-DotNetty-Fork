using System;

namespace EventStore.ClientAPI
{
    /// <summary>A stream events slice represents the result of a single read operation to Event Store.</summary>
    public class StreamEventsSlice<T> : StreamEventsSliceBase, IStreamEventsSlice<ResolvedEvent<T>>
    {
        /// <summary>The events read represented as <see cref="ResolvedEvent&lt;T&gt;"/></summary>
        public ResolvedEvent<T>[] Events { get; }

        internal StreamEventsSlice(SliceReadStatus status,
                                    string stream,
                                    long fromEventNumber,
                                    ReadDirection readDirection,
                                    ResolvedEvent<T>[] events,
                                    long nextEventNumber,
                                    long lastEventNumber,
                                    bool isEndOfStream)
            : base(status, stream, fromEventNumber, readDirection, nextEventNumber, lastEventNumber, isEndOfStream)
        {
            Events = events;
        }
    }

    /// <summary>A stream events slice represents the result of a single read operation to Event Store.</summary>
    public class StreamEventsSlice2 : StreamEventsSliceBase, IStreamEventsSlice<IResolvedEvent2>
    {
        /// <summary>The events read represented as <see cref="IResolvedEvent2"/></summary>
        public IResolvedEvent2[] Events { get; }

        internal StreamEventsSlice2(SliceReadStatus status,
                                    string stream,
                                    long fromEventNumber,
                                    ReadDirection readDirection,
                                    IResolvedEvent2[] events,
                                    long nextEventNumber,
                                    long lastEventNumber,
                                    bool isEndOfStream)
            : base(status, stream, fromEventNumber, readDirection, nextEventNumber, lastEventNumber, isEndOfStream)
        {
            Events = events;
        }
    }

    /// <summary>A stream events slice represents the result of a single read operation to Event Store.</summary>
    public class StreamEventsSliceBase
    {
        /// <summary>The <see cref="SliceReadStatus"/> representing the status of this read attempt.</summary>
        public SliceReadStatus Status { get; }

        /// <summary>The name of the stream to read.</summary>
        public string Stream { get; }

        /// <summary>The starting point (represented as a sequence number) of the read operation.</summary>
        public long FromEventNumber { get; }

        /// <summary>The direction of read request.</summary>
        public ReadDirection ReadDirection { get; }

        /// <summary>The next event number that can be read.</summary>
        public long NextEventNumber { get; }

        /// <summary>The last event number in the stream.</summary>
        public long LastEventNumber { get; }

        /// <summary>A boolean representing whether or not this is the end of the stream.</summary>
        public bool IsEndOfStream { get; }

        internal StreamEventsSliceBase(SliceReadStatus status,
                                        string stream,
                                        long fromEventNumber,
                                        ReadDirection readDirection,
                                        long nextEventNumber,
                                        long lastEventNumber,
                                        bool isEndOfStream)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }

            Status = status;
            Stream = stream;
            FromEventNumber = fromEventNumber;
            ReadDirection = readDirection;
            NextEventNumber = nextEventNumber;
            LastEventNumber = lastEventNumber;
            IsEndOfStream = isEndOfStream;
        }
    }
}