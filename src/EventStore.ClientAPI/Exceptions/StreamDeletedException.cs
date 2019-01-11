using System.Runtime.Serialization;

namespace EventStore.ClientAPI.Exceptions
{
    /// <summary>
    /// Exception thrown if an operation is attempted on a stream which
    /// has been deleted.
    /// </summary>
    public class StreamDeletedException : EventStoreConnectionException
    {
        /// <summary>
        /// The name of the deleted stream.
        /// </summary>
        public readonly string Stream;

        /// <summary>
        /// Constructs a new instance of <see cref="StreamDeletedException"/>.
        /// </summary>
        /// <param name="stream">The name of the deleted stream.</param>
        public StreamDeletedException(string stream)
            : base(string.Format("Event stream '{0}' is deleted.", stream))
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            Stream = stream;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="StreamDeletedException"/>.
        /// </summary>
        public StreamDeletedException()
            : base("Transaction failed due to underlying stream being deleted.")
        {
            Stream = null;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="StreamDeletedException"/>.
        /// </summary>
        protected StreamDeletedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
