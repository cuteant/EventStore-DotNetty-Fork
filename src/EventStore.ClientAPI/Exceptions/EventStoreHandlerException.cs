using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>EventStoreHandlerException</summary>
  public class EventStoreHandlerException : Exception
  {
    /// <summary>
    /// Constructs a new <see cref="EventStoreHandlerException"/>.
    /// </summary>
    public EventStoreHandlerException()
    {
    }

    /// <summary>
    /// Constructs a new <see cref="EventStoreHandlerException"/>.
    /// </summary>
    public EventStoreHandlerException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="EventStoreHandlerException"/>.
    /// </summary>
    public EventStoreHandlerException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if DESKTOPCLR
    /// <summary>
    /// Constructs a new <see cref="EventStoreHandlerException"/>.
    /// </summary>
    protected EventStoreHandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
  }
}
