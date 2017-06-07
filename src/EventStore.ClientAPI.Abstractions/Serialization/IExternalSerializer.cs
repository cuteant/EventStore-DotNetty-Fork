using System;

namespace EventStore.ClientAPI.Serialization
{
  public interface IExternalSerializer
  {
    /// <summary>Informs the serialization manager whether this serializer supports the type for serialization.</summary>
    /// <param name="itemType">The type of the item to be serialized</param>
    /// <returns>A value indicating whether the item can be serialized.</returns>
    bool IsSupportedType(Type itemType);

    /// <summary>Tries to serialize an item.</summary>
    /// <param name="item">The instance of the object being serialized</param>
    /// <returns>The serialization result</returns>
    byte[] Serialize(object item);

    /// <summary>Tries to deserialize an item.</summary>
    /// <param name="expectedType">The type that should be deserialized</param>
    /// <param name="data">The data which should be deserialized.</param>
    /// <returns>The deserialized object</returns>
    object Deserialize(Type expectedType, byte[] data);
  }

  /// <summary>EventSerializer</summary>
  public abstract class EventSerializer : IExternalSerializer
  {
    /// <summary>Informs the serialization manager whether this serializer supports the type for serialization.</summary>
    /// <param name="itemType">The type of the item to be serialized</param>
    /// <returns>A value indicating whether the item can be serialized.</returns>
    public abstract bool IsSupportedType(Type itemType);

    /// <summary>Tries to serialize an item.</summary>
    /// <param name="item">The instance of the object being serialized</param>
    /// <returns>The serialization result</returns>
    public abstract byte[] Serialize(object item);

    /// <summary>Tries to deserialize an item.</summary>
    /// <param name="expectedType">The type that should be deserialized</param>
    /// <param name="data">The data which should be deserialized.</param>
    /// <returns>The deserialized object</returns>
    public abstract object Deserialize(Type expectedType, byte[] data);
  }
}