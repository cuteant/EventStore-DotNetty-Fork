using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  /// <summary>Maintains a full duplex connection to the EventStore.</summary>
  /// <remarks>An <see cref="IEventStoreConnectionMultiplexer"/> operates quite differently than say a SqlConnection. Normally
  /// when using an <see cref="IEventStoreConnectionMultiplexer"/> you want to keep the connection open for a much longer of time than
  /// when you use a SqlConnection. If you prefer the usage pattern of using(new Connection()) .. then you would likely
  /// want to create a FlyWeight on top of the <see cref="IEventStoreConnectionMultiplexer"/>.
  ///
  /// Another difference is that with the <see cref="IEventStoreConnectionMultiplexer"/> all operations are handled in a full async manner
  /// (even if you call the synchronous behaviors). Many threads can use an <see cref="IEventStoreConnectionMultiplexer"/> at the same
  /// time or a single thread can make many asynchronous requests. To get the most performance out of the connection
  /// it is generally recommended to use it in this way.</remarks>
  public interface IEventStoreConnectionMultiplexer : IEventStoreBus
  {
    /// <summary>Continues transaction by provided transaction ID.</summary>
    /// <remarks>A <see cref="EventStoreTransaction"/> allows the calling of multiple writes with multiple
    /// round trips over long periods of time between the caller and the event store. This method
    /// is only available through the TCP interface and no equivalent exists for the RESTful interface.</remarks>
    /// <param name="stream">The stream to continue a transaction on</param>
    /// <param name="transactionId">The transaction ID that needs to be continued.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="EventStoreTransaction"/> representing a multi-request transaction.</returns>
    EventStoreTransaction ContinueTransaction(string stream, long transactionId, UserCredentials userCredentials = null);
  }
}
