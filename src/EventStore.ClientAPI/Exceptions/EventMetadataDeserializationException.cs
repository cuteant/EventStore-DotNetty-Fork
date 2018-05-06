using System;
using System.Runtime.Serialization;

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>Base type for exceptions thrown by an <see cref="IEventStoreConnection"/>,
  /// thrown in circumstances which do not have a specific derived exception.</summary>
  public class EventMetadataDeserializationException : Exception
  {
    /// <summary>Constructs a new <see cref="EventMetadataDeserializationException"/>.</summary>
    public EventMetadataDeserializationException()
    {
    }

    /// <summary>Constructs a new <see cref="EventMetadataDeserializationException"/>.</summary>
    public EventMetadataDeserializationException(string message) : base(message)
    {
    }

    /// <summary>Constructs a new <see cref="EventMetadataDeserializationException"/>.</summary>
    public EventMetadataDeserializationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Constructs a new <see cref="EventMetadataDeserializationException"/>.</summary>
    protected EventMetadataDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
