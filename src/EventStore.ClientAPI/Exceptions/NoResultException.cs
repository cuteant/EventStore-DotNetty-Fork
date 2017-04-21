using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>
  /// Exception thrown if there is no result for an operation for
  /// which one is expected.
  /// </summary>
  public class NoResultException : EventStoreConnectionException
  {
    /// <summary>
    /// Constructs a new <see cref="NoResultException"/>.
    /// </summary>
    public NoResultException()
    {
    }

    /// <summary>
    /// Constructs a new <see cref="NoResultException"/>.
    /// </summary>
    public NoResultException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="NoResultException"/>.
    /// </summary>
    public NoResultException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if DESKTOPCLR
    /// <summary>
    /// Constructs a new <see cref="NoResultException"/>.
    /// </summary>
    protected NoResultException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
  }
}
