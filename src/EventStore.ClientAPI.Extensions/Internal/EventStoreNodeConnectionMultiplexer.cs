using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class EventStoreNodeConnectionMultiplexer : IEventStoreConnectionMultiplexer
  {
    private readonly IList<IEventStoreConnection> _innerConnections;
    private readonly int _connectionCount;

    internal EventStoreNodeConnectionMultiplexer(IList<IEventStoreConnection> connections)
    {
      _innerConnections = connections;
      _connectionCount = connections.Count;
    }

    public Task ConnectAsync()
    {
      return Task.WhenAll(_innerConnections.Select(_ => _.ConnectAsync()).ToArray());
    }

    public void Close()
    {
      foreach (var conn in _innerConnections)
      {
        conn.Close();
      }
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].DeleteStreamAsync(stream, expectedVersion, userCredentials);
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].DeleteStreamAsync(stream, expectedVersion, hardDelete, userCredentials);
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, params EventData[] events)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].AppendToStreamAsync(stream, expectedVersion, events);
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials, params EventData[] events)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].AppendToStreamAsync(stream, expectedVersion, userCredentials, events);
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].AppendToStreamAsync(stream, expectedVersion, events, userCredentials);
    }

    public Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConditionalAppendToStreamAsync(stream, expectedVersion, events, userCredentials);
    }

    public Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].StartTransactionAsync(stream, expectedVersion, userCredentials);
    }

    public EventStoreTransaction ContinueTransaction(string stream, long transactionId, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ContinueTransaction(transactionId, userCredentials);
    }

    public Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ReadEventAsync(stream, eventNumber, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ReadStreamEventsBackwardAsync(stream, start, count, resolveLinkTos, userCredentials);
    }

    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos, Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamAsync(stream, resolveLinkTos, eventAppeared, subscriptionDropped, userCredentials);
    }

    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamAsync(stream, resolveLinkTos, eventAppeared, subscriptionDropped, userCredentials);
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConnectToPersistentSubscription(stream, groupName, eventAppeared, subscriptionDropped, userCredentials, bufferSize, autoAck);
    }

    public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConnectToPersistentSubscription(stream, groupName, eventAppeared, subscriptionDropped, userCredentials, bufferSize, autoAck);
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConnectToPersistentSubscriptionAsync(stream, groupName, eventAppeared, subscriptionDropped, userCredentials, bufferSize, autoAck);
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConnectToPersistentSubscriptionAsync(stream, groupName, eventAppeared, subscriptionDropped, userCredentials, bufferSize, autoAck);
    }

    public Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].UpdatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
    }


    public Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].CreatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
    }

    public Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].DeletePersistentSubscriptionAsync(stream, groupName, userCredentials);
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    public Task<StreamMetadataResult> GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamMetadataAsync(stream, userCredentials);
    }

    public Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamMetadataAsRawBytesAsync(stream, userCredentials);
    }

    public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null)
    {
      return _innerConnections[0].SetSystemSettingsAsync(settings, userCredentials);
    }

    public ConnectionSettings Settings => _innerConnections[0].Settings;
    void IDisposable.Dispose()
    {
      Close();
    }

    private static int CalculateConnectionIndex(string streamId, int count)
    {
      if (string.IsNullOrEmpty(streamId)) { throw new ArgumentNullException(nameof(streamId)); }

      return Math.Abs(streamId.GetHashCode() % count);
    }
  }
}
