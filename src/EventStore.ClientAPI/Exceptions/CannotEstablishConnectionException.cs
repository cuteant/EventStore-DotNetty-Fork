using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>
  /// Exception thrown if an <see cref="EventStoreConnection"/> is
  /// unable to establish a connection to an Event Store server.
  /// </summary>
  public class CannotEstablishConnectionException : EventStoreConnectionException
  {
    /// <summary>
    /// Constructs a new <see cref="CannotEstablishConnectionException" />.
    /// </summary>
    public CannotEstablishConnectionException()
    {
    }

    /// <summary>
    /// Constructs a new <see cref="CannotEstablishConnectionException" />.
    /// </summary>
    public CannotEstablishConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="CannotEstablishConnectionException" />.
    /// </summary>
    public CannotEstablishConnectionException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

#if DESKTOPCLR
    /// <summary>
    /// Constructs a new <see cref="CannotEstablishConnectionException" />.
    /// </summary>
    protected CannotEstablishConnectionException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
#endif
  }
}
