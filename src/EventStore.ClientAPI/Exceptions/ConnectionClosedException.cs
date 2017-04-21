using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>
  /// Exception thrown by ongoing operations which are terminated
  /// by an <see cref="IEventStoreConnection"/> closing.
  /// </summary>
  public class ConnectionClosedException : EventStoreConnectionException
  {
    /// <summary>
    /// Constructs a new <see cref="ConnectionClosedException" />.
    /// </summary>
    public ConnectionClosedException()
    {
    }

    /// <summary>
    /// Constructs a new <see cref="ConnectionClosedException" />.
    /// </summary>
    public ConnectionClosedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="ConnectionClosedException" />.
    /// </summary>
    public ConnectionClosedException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if DESKTOPCLR
    /// <summary>
    /// Constructs a new <see cref="ConnectionClosedException" />.
    /// </summary>
    protected ConnectionClosedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
  }
}