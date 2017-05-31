using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Authentication;
using EventStore.Core.Bus;
using EventStore.Core.Messages;

namespace EventStore.ClientAPI.Embedded
{
  internal partial class EventStoreEmbeddedNodeConnection : IEventStoreConnection, IEventStoreTransactionConnection
  {
    private class V8IntegrationAssemblyResolver
    {
      private readonly string _resourceNamespace;

      public V8IntegrationAssemblyResolver()
      {
        string environment = Environment.Is64BitProcess
            ? "x64"
            : "win32";

        _resourceNamespace = typeof(EventStoreEmbeddedNodeConnection).Namespace + ".libs." + environment;
      }

      public Assembly TryLoadAssemblyFromEmbeddedResource(object sender, ResolveEventArgs e)
      {
        if (!e.Name.StartsWith("js1", StringComparison.Ordinal)) return null;

        byte[] rawAssembly = ReadResource("js1.dll");
        byte[] rawSymbolStore = ReadResource("js1.pdb");

        return Assembly.Load(rawAssembly, rawSymbolStore);
      }


      private byte[] ReadResource(string name)
      {
        using (Stream stream = typeof(EventStoreEmbeddedNodeConnection)
            .Assembly
            .GetManifestResourceStream(_resourceNamespace + "." + name))
        {
          if (stream == null) throw new ArgumentNullException(nameof(name));

          var resource = new byte[stream.Length];

          stream.Read(resource, 0, resource.Length);

          return resource;
        }
      }
    }

    private readonly ConnectionSettings _settings;
    private readonly string _connectionName;
    private readonly IPublisher _publisher;
    private readonly IAuthenticationProvider _authenticationProvider;
    private readonly IBus _subscriptionBus;
    private readonly EmbeddedSubscriber _subscriptions;

    static EventStoreEmbeddedNodeConnection()
    {
      var resolver = new V8IntegrationAssemblyResolver();

      AppDomain.CurrentDomain.AssemblyResolve += resolver.TryLoadAssemblyFromEmbeddedResource;
    }

    public EventStoreEmbeddedNodeConnection(ConnectionSettings settings, string connectionName, IPublisher publisher, ISubscriber bus, IAuthenticationProvider authenticationProvider)
    {
      Ensure.NotNull(publisher, nameof(publisher));
      Ensure.NotNull(settings, nameof(settings));

      Guid connectionId = Guid.NewGuid();

      _settings = settings;
      _connectionName = connectionName;
      _publisher = publisher;
      _authenticationProvider = authenticationProvider;
      _subscriptionBus = new InMemoryBus("Embedded Client Subscriptions");
      _subscriptions = new EmbeddedSubscriber(_subscriptionBus, _authenticationProvider, connectionId);

      _subscriptionBus.Subscribe<ClientMessage.SubscriptionConfirmation>(_subscriptions);
      _subscriptionBus.Subscribe<ClientMessage.SubscriptionDropped>(_subscriptions);
      _subscriptionBus.Subscribe<ClientMessage.StreamEventAppeared>(_subscriptions);
      _subscriptionBus.Subscribe<ClientMessage.PersistentSubscriptionConfirmation>(_subscriptions);
      _subscriptionBus.Subscribe<ClientMessage.PersistentSubscriptionStreamEventAppeared>(_subscriptions);
      _subscriptionBus.Subscribe(new AdHocHandler<ClientMessage.SubscribeToStream>(_publisher.Publish));
      _subscriptionBus.Subscribe(new AdHocHandler<ClientMessage.UnsubscribeFromStream>(_publisher.Publish));
      _subscriptionBus.Subscribe(new AdHocHandler<ClientMessage.ConnectToPersistentSubscription>(_publisher.Publish));
      _subscriptionBus.Subscribe(new AdHocHandler<ClientMessage.PersistentSubscriptionAckEvents>(_publisher.Publish));
      _subscriptionBus.Subscribe(new AdHocHandler<ClientMessage.PersistentSubscriptionNackEvents>(_publisher.Publish));

      bus.Subscribe(new AdHocHandler<SystemMessage.BecomeShutdown>(_ => Disconnected(this, new ClientConnectionEventArgs(this, new IPEndPoint(IPAddress.None, 0)))));
    }

    public string ConnectionName => _connectionName;

    public ConnectionSettings Settings => _settings;

    public Task ConnectAsync()
    {
      var source = new TaskCompletionSource<object>();

      source.SetResult(null);

      Connected(this, new ClientConnectionEventArgs(this, new IPEndPoint(IPAddress.None, 0)));

      return source.Task;
    }

    public void Close()
    {
      _subscriptionBus.Unsubscribe<ClientMessage.SubscriptionConfirmation>(_subscriptions);
      _subscriptionBus.Unsubscribe<ClientMessage.SubscriptionDropped>(_subscriptions);
      _subscriptionBus.Unsubscribe<ClientMessage.StreamEventAppeared>(_subscriptions);

      Closed(this, new ClientClosedEventArgs(this, "Connection close requested by client."));
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      return DeleteStreamAsync(stream, expectedVersion, false, GetUserCredentials(_settings, userCredentials));
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));

      var source = new TaskCompletionSource<DeleteResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.DeleteStream(source, stream, expectedVersion));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.DeleteStream(corrId, corrId, envelope, false,
          stream, expectedVersion, hardDelete, user));

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
      return AppendToStreamAsync(stream, expectedVersion, (IEnumerable<EventData>)events, GetUserCredentials(_settings, userCredentials));
      // ReSharper restore RedundantCast
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      // ReSharper disable PossibleMultipleEnumeration
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(events, nameof(events));

      var source = new TaskCompletionSource<WriteResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.AppendToStream(source, stream, expectedVersion));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.WriteEvents(corrId, corrId, envelope, false,
          stream, expectedVersion, events.ConvertToEvents(), user));

      return source.Task;
      // ReSharper restore PossibleMultipleEnumeration
    }

    public Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      // ReSharper disable PossibleMultipleEnumeration
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(events, nameof(events));

      var source = new TaskCompletionSource<ConditionalWriteResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.ConditionalAppendToStream(source, stream));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.WriteEvents(corrId, corrId, envelope, false,
          stream, expectedVersion, events.ConvertToEvents(), user));

      return source.Task;
      // ReSharper restore PossibleMultipleEnumeration
    }

    public Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));

      var source = new TaskCompletionSource<EventStoreTransaction>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.TransactionStart(source, this, stream, expectedVersion));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.TransactionStart(corrId, corrId, envelope,
          false, stream, expectedVersion, user));

      return source.Task;
    }

    public EventStoreTransaction ContinueTransaction(long transactionId, UserCredentials userCredentials = null)
    {
      Ensure.Nonnegative(transactionId, nameof(transactionId));
      return new EventStoreTransaction(transactionId, GetUserCredentials(_settings, userCredentials), this);
    }

    Task IEventStoreTransactionConnection.TransactionalWriteAsync(EventStoreTransaction transaction, IEnumerable<EventData> events, UserCredentials userCredentials)
    {
      // ReSharper disable PossibleMultipleEnumeration
      Ensure.NotNull(transaction, nameof(transaction));
      Ensure.NotNull(events, nameof(events));

      var source = new TaskCompletionSource<EventStoreTransaction>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.TransactionWrite(source, this));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.TransactionWrite(corrId, corrId, envelope,
          false, transaction.TransactionId, events.ConvertToEvents(), user));

      return source.Task;

      // ReSharper restore PossibleMultipleEnumeration
    }

    Task<WriteResult> IEventStoreTransactionConnection.CommitTransactionAsync(EventStoreTransaction transaction, UserCredentials userCredentials)
    {
      Ensure.NotNull(transaction, nameof(transaction));

      var source = new TaskCompletionSource<WriteResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.TransactionCommit(source));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.TransactionCommit(corrId, corrId, envelope,
          false, transaction.TransactionId, user));

      return source.Task;
    }

    public Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      if (eventNumber < -1) throw new ArgumentOutOfRangeException(nameof(eventNumber));

      var source = new TaskCompletionSource<EventReadResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.ReadEvent(source, stream, eventNumber));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.ReadEvent(corrId, corrId, envelope,
          stream, eventNumber, resolveLinkTos, false, user));

      return source.Task;
    }

    public Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.Nonnegative(start, nameof(start));
      Ensure.Positive(count, nameof(count));
      if (count > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<StreamEventsSlice>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.ReadStreamForwardEvents(source, stream, start));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.ReadStreamEventsForward(corrId, corrId, envelope,
          stream, start, count, resolveLinkTos, false, null, user));

      return source.Task;
    }

    public Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.Positive(count, nameof(count));
      if (count > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<StreamEventsSlice>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.ReadStreamEventsBackward(source, stream, start));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.ReadStreamEventsBackward(corrId, corrId, envelope,
          stream, start, count, resolveLinkTos, false, null, user));

      return source.Task;
    }

    public Task<AllEventsSlice> ReadAllEventsForwardAsync(Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.Positive(maxCount, nameof(maxCount));
      if (maxCount > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<AllEventsSlice>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.ReadAllEventsForward(source));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.ReadAllEventsForward(corrId, corrId, envelope,
          position.CommitPosition,
          position.PreparePosition, maxCount, resolveLinkTos, false, null, user));

      return source.Task;
    }

    public Task<AllEventsSlice> ReadAllEventsBackwardAsync(Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      Ensure.Positive(maxCount, nameof(maxCount));
      if (maxCount > Consts.MaxReadSize) throw new ArgumentException(string.Format("Count should be less than {0}. For larger reads you should page.", Consts.MaxReadSize));
      var source = new TaskCompletionSource<AllEventsSlice>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.ReadAllEventsBackward(source));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.ReadAllEventsBackward(corrId, corrId, envelope,
          position.CommitPosition,
          position.PreparePosition, maxCount, resolveLinkTos, false, null, user));

      return source.Task;
    }
    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      var source = new TaskCompletionSource<EventStoreSubscription>();

      var corrId = Guid.NewGuid();

      _subscriptions.StartSubscription(corrId, source, stream, GetUserCredentials(_settings, userCredentials), settings.ResolveLinkTos, eventAppeared, subscriptionDropped);

      return source.Task;
    }
    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var source = new TaskCompletionSource<EventStoreSubscription>();

      var corrId = Guid.NewGuid();

      _subscriptions.StartSubscription(corrId, source, stream, GetUserCredentials(_settings, userCredentials), settings.ResolveLinkTos, eventAppearedAsync, subscriptionDropped);

      return source.Task;
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
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
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
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

    public Task<EventStoreSubscription> SubscribeToAllAsync(SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppeared, nameof(eventAppeared));

      var source = new TaskCompletionSource<EventStoreSubscription>();

      var corrId = Guid.NewGuid();

      _subscriptions.StartSubscription(corrId, source, string.Empty, GetUserCredentials(_settings, userCredentials),
          settings.ResolveLinkTos, eventAppeared, subscriptionDropped);

      return source.Task;
    }
    public Task<EventStoreSubscription> SubscribeToAllAsync(SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var source = new TaskCompletionSource<EventStoreSubscription>();

      var corrId = Guid.NewGuid();

      _subscriptions.StartSubscription(corrId, source, string.Empty, GetUserCredentials(_settings, userCredentials),
          settings.ResolveLinkTos, eventAppearedAsync, subscriptionDropped);

      return source.Task;
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var subscription = new EmbeddedEventStorePersistentSubscription(groupName, stream, settings, eventAppeared, subscriptionDropped,
          GetUserCredentials(_settings, userCredentials), _settings, _subscriptions);

      return subscription.StartAsync();
    }
    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var subscription = new EmbeddedEventStorePersistentSubscription(groupName, stream, settings, eventAppearedAsync, subscriptionDropped,
          GetUserCredentials(_settings, userCredentials), _settings, _subscriptions);

      return subscription.StartAsync();
    }

    public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
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
    public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
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

    public Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNull(settings, nameof(settings));

      var source = new TaskCompletionSource<PersistentSubscriptionCreateResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.CreatePersistentSubscription(source, stream, groupName));

      var corrId = Guid.NewGuid();


      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.CreatePersistentSubscription(
          corrId,
          corrId,
          envelope,
          stream,
          groupName,
          settings.ResolveLinkTos,
          settings.StartFrom,
          (int)settings.MessageTimeout.TotalMilliseconds,
          settings.ExtraStatistics,
          settings.MaxRetryCount,
          settings.HistoryBufferSize,
          settings.LiveBufferSize,
          settings.ReadBatchSize,
          (int)settings.CheckPointAfter.TotalMilliseconds,
          settings.MinCheckPointCount,
          settings.MaxCheckPointCount,
          settings.MaxSubscriberCount,
          settings.NamedConsumerStrategy,
          user,
          userCredentials?.Username,
          userCredentials?.Password));

      return source.Task;
    }

    public Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));

      var source = new TaskCompletionSource<PersistentSubscriptionUpdateResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.UpdatePersistentSubscription(source, stream, groupName));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.UpdatePersistentSubscription(
          corrId,
          corrId,
          envelope,
          stream,
          groupName,
          settings.ResolveLinkTos,
          settings.StartFrom,
          (int)settings.MessageTimeout.TotalMilliseconds,
          settings.ExtraStatistics,
          settings.MaxRetryCount,
          settings.HistoryBufferSize,
          settings.LiveBufferSize,
          settings.ReadBatchSize,
          (int)settings.CheckPointAfter.TotalMilliseconds,
          settings.MinCheckPointCount,
          settings.MaxCheckPointCount,
          settings.MaxSubscriberCount,
          settings.NamedConsumerStrategy,
          user,
          userCredentials?.Username,
          userCredentials?.Password));

      return source.Task;
    }


    public Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));

      var source = new TaskCompletionSource<PersistentSubscriptionDeleteResult>();

      var envelope = new EmbeddedResponseEnvelope(
          new EmbeddedResponders.DeletePersistentSubscription(source, stream, groupName));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.DeletePersistentSubscription(
          corrId,
          corrId,
          envelope,
          stream,
          groupName,
          user));

      return source.Task;
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      return SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata.AsJsonBytes(), GetUserCredentials(_settings, userCredentials));
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      if (SystemStreams.IsMetastream(stream))
        throw new ArgumentException($"Setting metadata for metastream '{stream}' is not supported.", nameof(stream));

      var metaevent = new EventData(Guid.NewGuid(), SystemEventTypes.StreamMetadata, true, metadata ?? Empty.ByteArray, null);
      var metastream = SystemStreams.MetastreamOf(stream);

      var source = new TaskCompletionSource<WriteResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.AppendToStream(source, metastream, expectedMetastreamVersion));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.WriteEvents(corrId, corrId, envelope, false, metastream,
          expectedMetastreamVersion, metaevent.ConvertToEvent(), user));

      return source.Task;
    }

    public Task<StreamMetadataResult> GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null)
    {
      return GetStreamMetadataAsRawBytesAsync(stream, GetUserCredentials(_settings, userCredentials)).ContinueWith(t =>
      {
        if (t.Exception != null) { throw t.Exception.InnerException; }
        var res = t.Result;
        if (res.StreamMetadata == null || res.StreamMetadata.Length == 0)
        {
          return new StreamMetadataResult(res.Stream, res.IsStreamDeleted, res.MetastreamVersion, StreamMetadata.Create());
        }
        var metadata = StreamMetadata.FromJsonBytes(res.StreamMetadata);
        return new StreamMetadataResult(res.Stream, res.IsStreamDeleted, res.MetastreamVersion, metadata);
      });
    }

    public Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream, UserCredentials userCredentials = null)
    {
      return ReadEventAsync(SystemStreams.MetastreamOf(stream), -1, false, GetUserCredentials(_settings, userCredentials)).ContinueWith(t =>
      {
        if (t.Exception != null) { throw t.Exception.InnerException; }

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
            throw new ArgumentOutOfRangeException($"Unexpected ReadEventResult: {res.Status}.");
        }
      });
    }

    public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null)
    {
      return AppendToStreamAsync(SystemStreams.SettingsStream, ExpectedVersion.Any, GetUserCredentials(_settings, userCredentials),
          new EventData(Guid.NewGuid(), SystemEventTypes.Settings, true, settings.ToJsonBytes(), null));
    }

    public event EventHandler<ClientConnectionEventArgs> Connected = delegate { };
    public event EventHandler<ClientConnectionEventArgs> Disconnected = delegate { };
    public event EventHandler<ClientReconnectingEventArgs> Reconnecting { add { } remove { } }
    public event EventHandler<ClientClosedEventArgs> Closed = delegate { };
    public event EventHandler<ClientErrorEventArgs> ErrorOccurred { add { } remove { } }
    public event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed { add { } remove { } }

    void IDisposable.Dispose() => Close();

    public Task TransactionalWriteAsync(EventStoreTransaction transaction, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      var source = new TaskCompletionSource<EventStoreTransaction>();

      var envelope = new EmbeddedResponseEnvelope(
          new EmbeddedResponders.TransactionWrite(source, this));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(_authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException, user => new ClientMessage.TransactionWrite(corrId, corrId, envelope, false,
          transaction.TransactionId, events.ConvertToEvents(), user));

      return source.Task;
    }

    public Task<WriteResult> CommitTransactionAsync(EventStoreTransaction transaction, UserCredentials userCredentials = null)
    {
      var source = new TaskCompletionSource<WriteResult>();

      var envelope = new EmbeddedResponseEnvelope(new EmbeddedResponders.TransactionCommit(source));

      var corrId = Guid.NewGuid();

      _publisher.PublishWithAuthentication(
          _authenticationProvider, GetUserCredentials(_settings, userCredentials), source.SetException,
          user => new ClientMessage.TransactionCommit(corrId, corrId, envelope, false,
              transaction.TransactionId, user));

      return source.Task;
    }

    private UserCredentials GetUserCredentials(ConnectionSettings settings, UserCredentials givenCredentials)
    {
      return givenCredentials ?? settings.DefaultUserCredentials;
    }
  }
}
