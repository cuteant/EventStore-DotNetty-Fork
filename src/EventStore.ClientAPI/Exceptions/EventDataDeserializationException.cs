using System;
using System.Runtime.Serialization;

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>Base type for exceptions thrown by an <see cref="IEventStoreConnection"/>,
  /// thrown in circumstances which do not have a specific derived exception.</summary>
  public class EventDataDeserializationException : Exception
  {
    /// <summary>Constructs a new <see cref="EventDataDeserializationException"/>.</summary>
    public EventDataDeserializationException()
    {
    }

    /// <summary>Constructs a new <see cref="EventDataDeserializationException"/>.</summary>
    public EventDataDeserializationException(string message) : base(message)
    {
    }

    /// <summary>Constructs a new <see cref="EventDataDeserializationException"/>.</summary>
    public EventDataDeserializationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Constructs a new <see cref="EventDataDeserializationException"/>.</summary>
    protected EventDataDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
