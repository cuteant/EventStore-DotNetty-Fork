namespace EventStore.ClientAPI
{
  /// <summary>IStreamCheckpointer</summary>
  public interface IStreamCheckpointer
  {
    long ProcessingEventNumber { get; }
  }
}
