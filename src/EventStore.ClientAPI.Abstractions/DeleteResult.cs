namespace EventStore.ClientAPI
{
  /// <summary>Result type returned after deleting a stream.</summary>
  public readonly struct DeleteResult
  {
    /// <summary>The <see cref="LogPosition"/> of the write.</summary>
    public readonly Position LogPosition;

    /// <summary>Constructs a new <see cref="DeleteResult"/>.</summary>
    /// <param name="logPosition">The position of the write in the log</param>
    public DeleteResult(in Position logPosition)
    {
      LogPosition = logPosition;
    }
  }
}