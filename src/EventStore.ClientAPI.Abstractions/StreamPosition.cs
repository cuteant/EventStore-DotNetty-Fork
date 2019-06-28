namespace EventStore.ClientAPI
{
  /// <summary>Constants for stream positions</summary>
  public static class StreamPosition
  {
    /// <summary>The first event in a stream</summary>
    public const long Start = 0L;

    /// <summary>The last event in the stream.</summary>
    public const long End = -1L;
  }
}