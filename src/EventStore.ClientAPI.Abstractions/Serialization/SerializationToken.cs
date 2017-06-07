namespace EventStore.ClientAPI.Serialization
{
  public enum SerializationToken
  {
    /// <summary>Using <c>Newtonsoft.Json</c> serialization.</summary>
    Json,

    /// <summary>Using <c>Newtonsoft.Json</c> serialization and the <c>GZip</c> loseless compression.</summary>
    GzJson,

    /// <summary>Using <c>Newtonsoft.Json</c> serialization and the <c>LZ4</c> loseless compression.</summary>
    Lz4Json,

    /// <summary>Using <c>Hyperion</c> serialization.</summary>
    Hyperion,

    /// <summary>Using <c>Hyperion</c> serialization and the <c>GZip</c> loseless compression.</summary>
    GzHyperion,

    /// <summary>Using <c>Hyperion</c> serialization and the <c>LZ4</c> loseless compression.</summary>
    Lz4Hyperion,

    /// <summary>Using <c>protobuf-net</c> serialization.</summary>
    Protobuf,

    /// <summary>Using <c>protobuf-net</c> serialization and the <c>GZip</c> loseless compression.</summary>
    GzProtobuf,

    /// <summary>Using <c>protobuf-net</c> serialization and the <c>LZ4</c> loseless compression.</summary>
    Lz4Protobuf,

    /// <summary>Using external serialization.</summary>
    External
  }
}