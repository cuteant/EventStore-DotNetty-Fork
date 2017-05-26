﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Internal
{
  /// <summary>
  /// Maintains a full duplex connection to the EventStore
  /// </summary>
  /// <remarks>
  /// An <see cref="EventStoreConnection"/> operates quite differently than say a <c>SqlConnection</c>. Normally
  /// when using an <see cref="EventStoreConnection"/> you want to keep the connection open for a much longer of time than
  /// when you use a SqlConnection. If you prefer the usage pattern of using(new Connection()) .. then you would likely
  /// want to create a FlyWeight on top of the <see cref="EventStoreConnection"/>.
  ///
  /// Another difference is that with the <see cref="EventStoreConnection"/> all operations are handled in a full async manner
  /// (even if you call the synchronous behaviors). Many threads can use an <see cref="EventStoreConnection"/> at the same
  /// time or a single thread can make many asynchronous requests. To get the most performance out of the connection
  /// it is generally recommended to use it in this way.
  /// </remarks>
  internal class EventStoreNodeConnection : IEventStoreConnection, IEventStoreTransactionConnection
  {
    public string ConnectionName { get { return _connectionName; } }

    private readonly string _connectionName;
    private readonly ConnectionSettings _settings;
    private readonly ClusterSettings _clusterSettings;
    private readonly IEndPointDiscoverer _endPointDiscoverer;
    private readonly EventStoreConnectionLogicHandler _handler;

    /// <summary>
    /// Returns the <see cref="ConnectionSettings"/> use to create this connection
    /// </summary>
    public ConnectionSettings Settings => _settings;

    /// <summary>
    /// Returns the <see cref="ClusterSettings"/> use to create this connection
    /// </summary>
    public ClusterSettings ClusterSettings => _clusterSettings;

    /// <summary>
    /// Constructs a new instance of a <see cref="EventStoreConnection"/>
    /// </summary>
    /// <param name="settings">The <see cref="ConnectionSettings"/> containing the settings for this connection.</param>
    /// <param name="clusterSettings">The <see cref="ClusterSettings" /> containing the settings for this connection.</param>
    /// <param name="endPointDiscoverer">Discoverer of destination node end point.</param>
    /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
    internal EventStoreNodeConnection(ConnectionSettings settings, ClusterSettings clusterSettings, IEndPointDiscoverer endPointDiscoverer, string connectionName)
    {
      Ensure.NotNull(settings, nameof(settings));
      Ensure.NotNull(endPointDiscoverer, nameof(endPointDiscoverer));

      _connectionName = connectionName ?? $"ES-{Guid.NewGuid()}";
      _settings = settings;
      _clusterSettings = clusterSettings;
      _endPointDiscoverer = endPointDiscoverer;
      _handler = new EventStoreConnectionLogicHandler(this, settings);
    }

    public Task ConnectAsync()
    {
      var source = new TaskCompletionSource<object>();
      _handler.EnqueueMessage(new StartConnectionMessage(source, _endPointDiscoverer));
      return source.Task;
    }

    void IDisposable.Dispose()
    {
      Close();
    }

    public void Close()
    {
      _handler.EnqueueMessage(new CloseConnectionMessage("Connection close requested by client.", null));
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      return DeleteStreamAsync(stream, expectedVersion, false, userCredentials);
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));

      var source = new TaskCompletionSource<DeleteResult>();
      EnqueueOperation(new DeleteStreamOperation(source, _settings.RequireMaster,
                                                 stream, expectedVersion, hardDelete, userCredentials));
      return source.Task;
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, params EventData[] events)
    {
      // ReSharper disable RedundantArgumentDefaultValue
      // ReSharper disable RedundantCast
      return AppendToStreamAsync(stream, expectedVersion, (IEnumerable<EventData>)events, null);
      // ReSharper restore RedundantCast
      // ReSharper restore RedundantArgumentDefaultValue
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials, params EventData[] events)
    {
      // ReSharper disable RedundantCast
      return AppendToStreamAsync(stream, expectedVersion, (IEnumerable<EventData>)events, userCredentials);
      // ReSharper restore RedundantCast
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      // ReSharper disable PossibleMultipleEnumeration
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(events, nameof(events));

      var source = new TaskCompletionSource<WriteResult>();
      EnqueueOperation(new AppendToStreamOperation(source, _settings.RequireMaster,
                                                   stream, expectedVersion, events, userCredentials));
      return source.Task;
      // ReSharper restore PossibleMultipleEnumeration
    }

    public Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events,
        UserCredentials userCredentials = null)
    {
      // ReSharper disable PossibleMultipleEnumeration
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(events, nameof(events));

      var source = new TaskCompletionSource<ConditionalWriteResult>();
      EnqueueOperation(new ConditionalAppendToStreamOperation(source, _settings.RequireMaster,
                                                   stream, expectedVersion, events, userCredentials));
      return source.Task;
      // ReSharper restore PossibleMultipleEnumeration
    }

    public Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));

      var source = new TaskCompletionSource<EventStoreTransaction>();
      EnqueueOperation(new StartTransactionOperation(source, _settings.RequireMaster,
                                                     stream, expectedVersion, this, userCredentials));
      return source.Task;
    }

    public EventStoreTransaction ContinueTransaction(long transactionId, UserCredentials userCredentials = null)
    {
      Ensure.Nonnegative(transactionId, nameof(transactionId));
      return new EventStoreTransaction(transactionId, userCredentials, this);
    }

    Task IEventStoreTransactionConnection.TransactionalWriteAsync(EventStoreTransaction transaction, IEnumerable<EventData> events, UserCredentials userCredentials)
    {
      // ReSharper disable PossibleMultipleEnumeration
      Ensure.NotNull(transaction, nameof(transaction));
      Ensure.NotNull(events, nameof(events));

      var source = new TaskCompletionSource<object>();
      EnqueueOperation(new TransactionalWriteOperation(source, _settings.RequireMaster,
                                                       transaction.TransactionId, events, userCredentials));
      return source.Task;
      // ReSharper restore PossibleMultipleEnumeration
    }

    Task<WriteResult> IEventStoreTransactionConnection.CommitTransactionAsync(EventStoreTransaction transaction, UserCredentials userCredentials)
    {
      Ensure.NotNull(transaction, nameof(transaction));

      var source = new TaskCompletionSource<WriteResult>();
      EnqueueOperation(new CommitTransactionOperation(source, _settings.RequireMaster,
                                                      transaction.TransactionId, userCredentials));
      return source.Task;
    }


    public Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      if (eventNumber < -1) throw new ArgumentOutOfRangeException(nameof(eventNumber));
      var source = new TaskCompletionSource<EventReadResult>();
      var operation = new ReadEventOperation(source, stream, eventNumber, resolveLinkTos,
                                             _settings.RequireMaster, userCredentials);
      EnqueueOperation(operation);
      return source.Task;
    }

    public Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.Nonnegative(start, nameof(start));
      Ensure.Positive(count, nameof(count));
      if (count > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<StreamEventsSlice>();
      var operation = new ReadStreamEventsForwardOperation(source, stream, start, count,
                                                           resolveLinkTos, _settings.RequireMaster, userCredentials);
      EnqueueOperation(operation);
      return source.Task;
    }

    public Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.Positive(count, nameof(count));
      if (count > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<StreamEventsSlice>();
      var operation = new ReadStreamEventsBackwardOperation(source, stream, start, count,
                                                            resolveLinkTos, _settings.RequireMaster, userCredentials);
      EnqueueOperation(operation);
      return source.Task;
    }

    public Task<AllEventsSlice> ReadAllEventsForwardAsync(Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.Positive(maxCount, nameof(maxCount));
      if (maxCount > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<AllEventsSlice>();
      var operation = new ReadAllEventsForwardOperation(source, position, maxCount,
                                                        resolveLinkTos, _settings.RequireMaster, userCredentials);
      EnqueueOperation(operation);
      return source.Task;
    }

    public Task<AllEventsSlice> ReadAllEventsBackwardAsync(Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.Positive(maxCount, nameof(maxCount));
      if (maxCount > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<AllEventsSlice>();
      var operation = new ReadAllEventsBackwardOperation(source, position, maxCount,
                                                         resolveLinkTos, _settings.RequireMaster, userCredentials);
      EnqueueOperation(operation);
      return source.Task;
    }

    private void EnqueueOperation(IClientOperation operation)
    {
      if (_handler.TotalOperationCount >= _settings.MaxQueueSize)
      {
        var spinner = new SpinWait();
        while (_handler.TotalOperationCount >= _settings.MaxQueueSize)
        {
          spinner.SpinOnce();
        }
      }
      _handler.EnqueueMessage(new StartOperationMessage(operation, _settings.MaxRetries, _settings.OperationTimeout));
    }

    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      var source = new TaskCompletionSource<EventStoreSubscription>();
      _handler.EnqueueMessage(new StartSubscriptionMessage(source, stream, resolveLinkTos, userCredentials,
                                                           eventAppeared, subscriptionDropped,
                                                           _settings.MaxRetries, _settings.OperationTimeout));
      return source.Task;
    }
    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var source = new TaskCompletionSource<EventStoreSubscription>();
      _handler.EnqueueMessage(new StartSubscriptionMessage(source, stream, resolveLinkTos, userCredentials,
                                                           eventAppearedAsync, subscriptionDropped,
                                                           _settings.MaxRetries, _settings.OperationTimeout));
      return source.Task;
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream,
                                                                     long? lastCheckpoint,
                                                                     bool resolveLinkTos,
                                                                     Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
                                                                     Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
                                                                     Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
                                                                     UserCredentials userCredentials = null,
                                                                     int readBatchSize = 500,
                                                                     string subscriptionName = "")
    {
      var settings = new CatchUpSubscriptionSettings(Consts.CatchUpDefaultMaxPushQueueSize, readBatchSize,
                                                                              _settings.VerboseLogging,
                                                                              resolveLinkTos,
                                                                              subscriptionName);
      return SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(settings, nameof(settings));
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));
      var catchUpSubscription =
              new EventStoreStreamCatchUpSubscription(this, stream, lastCheckpoint,
                                                      userCredentials, eventAppeared, liveProcessingStarted,
                                                      subscriptionDropped, settings);
      catchUpSubscription.StartAsync();
      return catchUpSubscription;
    }
    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(settings, nameof(settings));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));
      var catchUpSubscription =
              new EventStoreStreamCatchUpSubscription(this, stream, lastCheckpoint,
                                                      userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                      subscriptionDropped, settings);
      catchUpSubscription.StartAsync();
      return catchUpSubscription;
    }

    public Task<EventStoreSubscription> SubscribeToAllAsync(
            bool resolveLinkTos,
            Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      var source = new TaskCompletionSource<EventStoreSubscription>();
      _handler.EnqueueMessage(new StartSubscriptionMessage(source, string.Empty, resolveLinkTos, userCredentials,
                                                           eventAppeared, subscriptionDropped,
                                                           _settings.MaxRetries, _settings.OperationTimeout));
      return source.Task;
    }
    public Task<EventStoreSubscription> SubscribeToAllAsync(
            bool resolveLinkTos,
            Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var source = new TaskCompletionSource<EventStoreSubscription>();
      _handler.EnqueueMessage(new StartSubscriptionMessage(source, string.Empty, resolveLinkTos, userCredentials,
                                                           eventAppearedAsync, subscriptionDropped,
                                                           _settings.MaxRetries, _settings.OperationTimeout));
      return source.Task;
    }

    public EventStoreAllCatchUpSubscription SubscribeToAllFrom(
        Position? lastCheckpoint,
        bool resolveLinkTos,
        Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
        Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
        Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
        UserCredentials userCredentials = null,
        int readBatchSize = 500,
        string subscriptionName = "")
    {
      var settings = new CatchUpSubscriptionSettings(Consts.CatchUpDefaultMaxPushQueueSize, readBatchSize,
                                                                              _settings.VerboseLogging, resolveLinkTos, subscriptionName);
      return SubscribeToAllFrom(lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    public EventStoreAllCatchUpSubscription SubscribeToAllFrom(
        Position? lastCheckpoint,
        CatchUpSubscriptionSettings settings,
        Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
        Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
        Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
        UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));
      Ensure.NotNull(settings, nameof(settings));
      var catchUpSubscription =
              new EventStoreAllCatchUpSubscription(this, lastCheckpoint,
                                                   userCredentials, eventAppeared, liveProcessingStarted,
                                                   subscriptionDropped, settings);
      catchUpSubscription.StartAsync();
      return catchUpSubscription;
    }
    public EventStoreAllCatchUpSubscription SubscribeToAllFrom(
        Position? lastCheckpoint,
        CatchUpSubscriptionSettings settings,
        Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
        Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
        Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
        UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));
      Ensure.NotNull(settings, nameof(settings));
      var catchUpSubscription =
              new EventStoreAllCatchUpSubscription(this, lastCheckpoint,
                                                   userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                   subscriptionDropped, settings);
      catchUpSubscription.StartAsync();
      return catchUpSubscription;
    }

    public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      var subscription = new EventStorePersistentSubscription(
          groupName, stream, eventAppeared, subscriptionDropped, userCredentials,
          _settings.VerboseLogging, _settings, _handler, bufferSize, autoAck);

      subscription.Start().Wait();
      return subscription;
    }
    public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var subscription = new EventStorePersistentSubscription(
          groupName, stream, eventAppearedAsync, subscriptionDropped, userCredentials,
          _settings.VerboseLogging, _settings, _handler, bufferSize, autoAck);

      subscription.Start().Wait();
      return subscription;
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      var subscription = new EventStorePersistentSubscription(
          groupName, stream, eventAppeared, subscriptionDropped, userCredentials,
          _settings.VerboseLogging, _settings, _handler, bufferSize, autoAck);

      return subscription.Start();
    }
    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var subscription = new EventStorePersistentSubscription(
          groupName, stream, eventAppearedAsync, subscriptionDropped, userCredentials,
          _settings.VerboseLogging, _settings, _handler, bufferSize, autoAck);

      return subscription.Start();
    }
    /*

            public EventStorePersistentSubscription ConnectToPersistentSubscriptionForAll(
                string groupName,
                Action<EventStorePersistentSubscription, ResolvedEvent> eventAppeared,
                Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
                UserCredentials userCredentials = null,
                int? bufferSize = null,
                bool autoAck = true)
            {
                return ConnectToPersistentSubscription(groupName,
                    SystemStreams.AllStream,
                    eventAppeared,
                    subscriptionDropped,
                    userCredentials,
                    bufferSize,
                    autoAck);
            }
    */


    public Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNull(settings, nameof(settings));
      var source = new TaskCompletionSource<PersistentSubscriptionCreateResult>();
      EnqueueOperation(new CreatePersistentSubscriptionOperation(source, stream, groupName, settings, userCredentials));
      return source.Task;
    }

    public Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNull(settings, nameof(settings));
      var source = new TaskCompletionSource<PersistentSubscriptionUpdateResult>();
      EnqueueOperation(new UpdatePersistentSubscriptionOperation(source, stream, groupName, settings, userCredentials));
      return source.Task;
    }
    /*

            public Task<PersistentSubscriptionCreateResult> CreatePersistentSubscriptionForAllAsync(string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials = null)
            {
                Ensure.NotNullOrEmpty(groupName, nameof(groupName));
                Ensure.NotNull(settings, nameof(settings));
                var source = new TaskCompletionSource<PersistentSubscriptionCreateResult>();
                EnqueueOperation(new CreatePersistentSubscriptionOperation(_settings.Log, source, SystemStreams.AllStream, groupName, settings, userCredentials));
                return source.Task;
            }

    */
    public Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      var source = new TaskCompletionSource<PersistentSubscriptionDeleteResult>();
      EnqueueOperation(new DeletePersistentSubscriptionOperation(source, stream, groupName, userCredentials));
      return source.Task;
    }
    /*

            public Task<PersistentSubscriptionDeleteResult> DeletePersistentSubscriptionForAllAsync(string groupName, UserCredentials userCredentials = null)
            {
                Ensure.NotNullOrEmpty(groupName, nameof(groupName));
                var source = new TaskCompletionSource<PersistentSubscriptionDeleteResult>();
                EnqueueOperation(new DeletePersistentSubscriptionOperation(_settings.Log, source, SystemStreams.AllStream, groupName, userCredentials));
                return source.Task;
            }

    */

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      return SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata.AsJsonBytes(), userCredentials);
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      if (SystemStreams.IsMetastream(stream))
      {
        throw new ArgumentException($"Setting metadata for metastream '{stream}' is not supported.", nameof(stream));
      }

      var source = new TaskCompletionSource<WriteResult>();

      var metaevent = new EventData(Guid.NewGuid(), SystemEventTypes.StreamMetadata, true, metadata ?? Empty.ByteArray, null);
      EnqueueOperation(new AppendToStreamOperation(source,
                                                   _settings.RequireMaster,
                                                   SystemStreams.MetastreamOf(stream),
                                                   expectedMetastreamVersion,
                                                   new[] { metaevent },
                                                   userCredentials));
      return source.Task;
    }

    public Task<StreamMetadataResult> GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null)
    {
      return GetStreamMetadataAsRawBytesAsync(stream, userCredentials).ContinueWith(t =>
      {
        if (t.Exception != null)
          throw t.Exception.InnerException;
        var res = t.Result;
        if (res.StreamMetadata == null || res.StreamMetadata.Length == 0)
          return new StreamMetadataResult(res.Stream, res.IsStreamDeleted, res.MetastreamVersion, StreamMetadata.Create());
        var metadata = StreamMetadata.FromJsonBytes(res.StreamMetadata);
        return new StreamMetadataResult(res.Stream, res.IsStreamDeleted, res.MetastreamVersion, metadata);
      });
    }

    public Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream, UserCredentials userCredentials = null)
    {
      return ReadEventAsync(SystemStreams.MetastreamOf(stream), -1, false, userCredentials).ContinueWith(t =>
      {
        if (t.Exception != null)
          throw t.Exception.InnerException;

        var res = t.Result;
        switch (res.Status)
        {
          case EventReadStatus.Success:
            if (res.Event == null) throw new Exception("Event is null while operation result is Success.");
            var evnt = res.Event.Value.OriginalEvent;
            if (evnt == null) return new RawStreamMetadataResult(stream, false, -1, Empty.ByteArray);
            return new RawStreamMetadataResult(stream, false, evnt.EventNumber, evnt.Data);
          case EventReadStatus.NotFound:
          case EventReadStatus.NoStream:
            return new RawStreamMetadataResult(stream, false, -1, Empty.ByteArray);
          case EventReadStatus.StreamDeleted:
            return new RawStreamMetadataResult(stream, true, long.MaxValue, Empty.ByteArray);
          default:
            throw new ArgumentOutOfRangeException(string.Format("Unexpected ReadEventResult: {0}.", res.Status));
        }
      });
    }

    public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null)
    {
      return AppendToStreamAsync(SystemStreams.SettingsStream, ExpectedVersion.Any, userCredentials,
                                 new EventData(Guid.NewGuid(), SystemEventTypes.Settings, true, settings.ToJsonBytes(), null));
    }

    public event EventHandler<ClientConnectionEventArgs> Connected
    {
      add
      {
        _handler.Connected += value;
      }
      remove
      {
        _handler.Connected -= value;
      }
    }

    public event EventHandler<ClientConnectionEventArgs> Disconnected
    {
      add
      {
        _handler.Disconnected += value;
      }
      remove
      {
        _handler.Disconnected -= value;
      }
    }

    public event EventHandler<ClientReconnectingEventArgs> Reconnecting
    {
      add
      {
        _handler.Reconnecting += value;
      }
      remove
      {
        _handler.Reconnecting -= value;
      }
    }

    public event EventHandler<ClientClosedEventArgs> Closed
    {
      add
      {
        _handler.Closed += value;
      }
      remove
      {
        _handler.Closed -= value;
      }
    }

    public event EventHandler<ClientErrorEventArgs> ErrorOccurred
    {
      add
      {
        _handler.ErrorOccurred += value;
      }
      remove
      {
        _handler.ErrorOccurred -= value;
      }
    }

    public event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed
    {
      add
      {
        _handler.AuthenticationFailed += value;
      }
      remove
      {
        _handler.AuthenticationFailed -= value;
      }
    }
  }
}