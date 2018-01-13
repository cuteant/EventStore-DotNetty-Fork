namespace EventStore.ClientAPI.Serialization
{
  public enum EventSerializingToken
  {
    /// <summary>Using <c>Newtonsoft.Json</c> serialization.</summary>
    Json,

    /// <summary>Using <c>Utf8Json</c> serialization.</summary>
    Utf8Json,

    /// <summary>Using <c>MessagePack</c> serialization.</summary>
    MessagePack,

    /// <summary>Using <c>MessagePack</c> serialization and the <c>LZ4</c> loseless compression.</summary>
    Lz4MessagePack,

    /// <summary>Using external serialization.</summary>
    External
  }
}