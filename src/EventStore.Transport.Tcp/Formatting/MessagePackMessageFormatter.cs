using System;
using System.IO;
using EventStore.BufferManagement;
using MessagePack;

namespace EventStore.Transport.Tcp.Formatting
{
    /// <summary>
    /// Formats a message for transport using MessagePack serialization
    /// </summary>
    public class MessagePackMessageFormatter<T> : FormatterBase<T>
    {
        private readonly BufferManager _bufferManager;
        private readonly int _initialBuffers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackMessageFormatter{T}"/> class.
        /// </summary>
        public MessagePackMessageFormatter() : this(BufferManager.Default, 2) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackMessageFormatter{T}"/> class.
        /// </summary>
        /// <param name="bufferManager">The buffer manager.</param>
        public MessagePackMessageFormatter(BufferManager bufferManager) : this(bufferManager, 2) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackMessageFormatter{T}"/> class.
        /// </summary>
        /// <param name="bufferManager">The buffer manager.</param>
        /// <param name="initialBuffers">The number of initial buffers.</param>
        public MessagePackMessageFormatter(BufferManager bufferManager, int initialBuffers)
        {
            _bufferManager = bufferManager;
            _initialBuffers = initialBuffers;
        }

        /// <summary>
        /// Gets a <see cref="BufferPool"></see> representing the IMessage provided.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="BufferPool"></see> with a representation of the message</returns>
        public override BufferPool ToBufferPool(T message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var bufferPool = new BufferPool(_initialBuffers, _bufferManager);
            var stream = new BufferPoolStream(bufferPool);
            MessagePackSerializer.Serialize(stream, message);
            return bufferPool;
        }

        /// <summary>
        /// Creates a message object from the specified stream
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A message object</returns>
        public override T From(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            return MessagePackSerializer.Deserialize<T>(stream);
        }
    }
}