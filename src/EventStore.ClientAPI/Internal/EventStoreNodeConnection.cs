using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Internal
{
    /// <summary>Maintains a full duplex connection to Event Store</summary>
    /// <remarks>An <see cref="EventStoreConnection"/> operates quite differently than say a <c>SqlConnection</c>. Normally
    /// when using an <see cref="EventStoreConnection"/> you want to keep the connection open for a much longer of time than
    /// when you use a SqlConnection. If you prefer the usage pattern of using(new Connection()) .. then you would likely
    /// want to create a FlyWeight on top of the <see cref="EventStoreConnection"/>.
    ///
    /// Another difference is that with the <see cref="EventStoreConnection"/> all operations are handled in a full async manner
    /// (even if you call the synchronous behaviors). Many threads can use an <see cref="EventStoreConnection"/> at the same
    /// time or a single thread can make many asynchronous requests. To get the most performance out of the connection
    /// it is generally recommended to use it in this way.</remarks>
    internal class EventStoreNodeConnection : IEventStoreConnection2, IEventStoreTransactionConnection
    {
        #region @@ Fields @@

        private readonly string _connectionName;
        private readonly ConnectionSettings _settings;
        private readonly ClusterSettings _clusterSettings;
        private readonly IEndPointDiscoverer _endPointDiscoverer;
        private readonly EventStoreConnectionLogicHandler _handler;

        #endregion

        #region @@ Properties @@

        public string ConnectionName { get { return _connectionName; } }

        /// <summary>Returns the <see cref="ConnectionSettings"/> use to create this connection.</summary>
        public ConnectionSettings Settings => _settings;

        /// <summary>Returns the <see cref="ClusterSettings"/> use to create this connection.</summary>
        public ClusterSettings ClusterSettings => _clusterSettings;

        #endregion

        #region @@ Constructors @@

        /// <summary>Constructs a new instance of a <see cref="EventStoreConnection"/>.</summary>
        /// <param name="settings">The <see cref="ConnectionSettings"/> containing the settings for this connection.</param>
        /// <param name="clusterSettings">The <see cref="ClusterSettings" /> containing the settings for this connection.</param>
        /// <param name="endPointDiscoverer">Discoverer of destination node end point.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        internal EventStoreNodeConnection(ConnectionSettings settings, ClusterSettings clusterSettings, IEndPointDiscoverer endPointDiscoverer, string connectionName)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == endPointDiscoverer) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPointDiscoverer); }

            _connectionName = connectionName ?? $"ES-{Guid.NewGuid()}";
            _settings = settings;
            _clusterSettings = clusterSettings;
            _endPointDiscoverer = endPointDiscoverer;
            _handler = new EventStoreConnectionLogicHandler(this, settings);
        }

        #endregion

        #region -- ConnectAsync --

        public Task ConnectAsync()
        {
            var source = TaskCompletionSourceFactory.Create<object>();
            _handler.EnqueueMessage(new StartConnectionMessage(source, _endPointDiscoverer));
            return source.Task;
        }

        #endregion

        #region -- IDisposable Members --

        void IDisposable.Dispose()
        {
            Close();
        }

        #endregion

        #region -- Close --

        public void Close()
        {
            _handler.EnqueueMessage(new CloseConnectionMessage("Connection close requested by client.", null));
        }

        #endregion

        #region -- DeleteStreamAsync --

        public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
        {
            return DeleteStreamAsync(stream, expectedVersion, false, userCredentials);
        }

        public async Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }

            var source = TaskCompletionSourceFactory.Create<DeleteResult>();
            EnqueueOperation(new DeleteStreamOperation(source, _settings.RequireMaster,
                                                       stream, expectedVersion, hardDelete, userCredentials));
            return await source.Task.ConfigureAwait(false);
        }

        #endregion

        #region -- AppendToStreamAsync / ConditionalAppendToStreamAsync --

        public async Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, EventData evt, UserCredentials userCredentials = null)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == evt) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evt); }

            var source = TaskCompletionSourceFactory.Create<WriteResult>();
            EnqueueOperation(new AppendToStreamOperation(source, _settings.RequireMaster,
                                                         stream, expectedVersion, evt, userCredentials));
            return await source.Task.ConfigureAwait(false);
            // ReSharper restore PossibleMultipleEnumeration
        }

        public async Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var source = TaskCompletionSourceFactory.Create<WriteResult>();
            EnqueueOperation(new AppendToStreamOperation(source, _settings.RequireMaster,
                                                         stream, expectedVersion, events, userCredentials));
            return await source.Task.ConfigureAwait(false);
            // ReSharper restore PossibleMultipleEnumeration
        }

        public async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, EventData evt,
            UserCredentials userCredentials = null)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == evt) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evt); }

            var source = TaskCompletionSourceFactory.Create<ConditionalWriteResult>();
            EnqueueOperation(new ConditionalAppendToStreamOperation(source, _settings.RequireMaster,
                                                                    stream, expectedVersion, evt, userCredentials));
            return await source.Task.ConfigureAwait(false);
            // ReSharper restore PossibleMultipleEnumeration
        }

        public async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events,
            UserCredentials userCredentials = null)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var source = TaskCompletionSourceFactory.Create<ConditionalWriteResult>();
            EnqueueOperation(new ConditionalAppendToStreamOperation(source, _settings.RequireMaster,
                                                                    stream, expectedVersion, events, userCredentials));
            return await source.Task.ConfigureAwait(false);
            // ReSharper restore PossibleMultipleEnumeration
        }

        #endregion

        #region -- Transaction --

        public async Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }

            var source = TaskCompletionSourceFactory.Create<EventStoreTransaction>();
            EnqueueOperation(new StartTransactionOperation(source, _settings.RequireMaster,
                                                           stream, expectedVersion, this, userCredentials));
            return await source.Task.ConfigureAwait(false);
        }

        public EventStoreTransaction ContinueTransaction(long transactionId, UserCredentials userCredentials = null)
        {
            if (transactionId < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.transactionId); }
            return new EventStoreTransaction(transactionId, userCredentials, this);
        }

        async Task IEventStoreTransactionConnection.TransactionalWriteAsync(EventStoreTransaction transaction, IEnumerable<EventData> events, UserCredentials userCredentials)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (null == transaction) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.transaction); }
            if (null == events) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.events); }

            var source = TaskCompletionSourceFactory.Create<object>();
            EnqueueOperation(new TransactionalWriteOperation(source, _settings.RequireMaster,
                                                             transaction.TransactionId, events, userCredentials));
            await source.Task.ConfigureAwait(false);
            // ReSharper restore PossibleMultipleEnumeration
        }

        async Task<WriteResult> IEventStoreTransactionConnection.CommitTransactionAsync(EventStoreTransaction transaction, UserCredentials userCredentials)
        {
            if (null == transaction) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.transaction); }

            var source = TaskCompletionSourceFactory.Create<WriteResult>();
            EnqueueOperation(new CommitTransactionOperation(source, _settings.RequireMaster,
                                                            transaction.TransactionId, userCredentials));
            return await source.Task.ConfigureAwait(false);
        }

        #endregion

        #region -- Read event(s) --

        public async Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (eventNumber < -1) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventNumber); }
            var source = TaskCompletionSourceFactory.Create<EventReadResult>();
            var operation = new ReadRawEventOperation(source, stream, eventNumber, resolveLinkTos,
                                                      _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        public async Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (start < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.start); }
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice>();
            var operation = new ReadRawStreamEventsForwardOperation(source, stream, start, count,
                                                                    resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        public async Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (start < StreamPosition.End) { ThrowHelper.ThrowArgumentOutOfRangeException_GreaterThanOrEqualTo(StreamPosition.End, ExceptionArgument.start); }
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice>();
            var operation = new ReadRawStreamEventsBackwardOperation(source, stream, start, count,
                                                                     resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        public Task<AllEventsSlice> ReadAllEventsForwardAsync(in Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (maxCount <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.maxCount); }
            if (maxCount > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<AllEventsSlice>();
            var operation = new ReadAllEventsForwardOperation(source, position, maxCount,
                                                              resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return source.Task;
        }

        public Task<AllEventsSlice> ReadAllEventsBackwardAsync(in Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (maxCount <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.maxCount); }
            if (maxCount > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<AllEventsSlice>();
            var operation = new ReadAllEventsBackwardOperation(source, position, maxCount,
                                                               resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return source.Task;
        }

        #endregion


        #region -- GetEventAsync --

        public async Task<EventReadResult<object>> GetEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (eventNumber < -1) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventNumber); }
            var source = TaskCompletionSourceFactory.Create<EventReadResult<object>>();
            var operation = new ReadEventOperation(source, stream, eventNumber, resolveLinkTos,
                                                   _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        public async Task<EventReadResult<TEvent>> GetEventAsync<TEvent>(string topic, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (eventNumber < -1) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.eventNumber); }
            var stream = EventManager.GetStreamId<TEvent>(topic);
            var source = TaskCompletionSourceFactory.Create<EventReadResult<TEvent>>();
            var operation = new ReadEventOperation<TEvent>(source, stream, eventNumber, resolveLinkTos,
                                                           _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        #endregion

        #region -- GetStreamEventsForwardAsync --

        public async Task<StreamEventsSlice<object>> GetStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (start < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.start); }
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice<object>>();
            var operation = new ReadStreamEventsForwardOperation(source, stream, start, count,
                                                                 resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }
        public async Task<StreamEventsSlice2> InternalGetStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (start < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.start); }
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice2>();
            var operation = new ReadStreamEventsForwardOperation2(source, stream, start, count,
                                                                  resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        public async Task<StreamEventsSlice<TEvent>> GetStreamEventsForwardAsync<TEvent>(string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (start < 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Nonnegative(ExceptionArgument.start); }
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();

            var stream = EventManager.GetStreamId<TEvent>(topic);
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice<TEvent>>();
            var operation = new ReadStreamEventsForwardOperation<TEvent>(source, stream, start, count,
                                                                         resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        #endregion

        #region -- GetStreamEventsBackwardAsync --

        public async Task<StreamEventsSlice<object>> GetStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice<object>>();
            var operation = new ReadStreamEventsBackwardOperation(source, stream, start, count,
                                                                  resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        public async Task<StreamEventsSlice<TEvent>> GetStreamEventsBackwardAsync<TEvent>(string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
        {
            if (count <= 0) { ThrowHelper.ThrowArgumentOutOfRangeException_Positive(ExceptionArgument.count); }
            if (count > ClientApiConstants.MaxReadSize) CoreThrowHelper.ThrowArgumentException_CountShouldBeLessThanMaxReadSize();

            var stream = EventManager.GetStreamId<TEvent>(topic);
            var source = TaskCompletionSourceFactory.Create<StreamEventsSlice<TEvent>>();
            var operation = new ReadStreamEventsBackwardOperation<TEvent>(source, stream, start, count,
                                                                          resolveLinkTos, _settings.RequireMaster, userCredentials);
            EnqueueOperation(operation);
            return await source.Task.ConfigureAwait(false);
        }

        #endregion


        #region ** EnqueueOperation **

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

        #endregion


        #region -- VolatileSubscribeAsync  --

        public Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings,
          Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionMessage(source, stream, settings, userCredentials,
                                                                 eventAppeared, subscriptionDropped,
                                                                 _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }
        public Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings,
          Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionMessage(source, stream, settings, userCredentials,
                                                                 eventAppearedAsync, subscriptionDropped,
                                                                 _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }
        public Task<EventStoreSubscription> InternalVolatileSubscribeAsync(string stream, SubscriptionSettings settings,
          Func<EventStoreSubscription, IResolvedEvent2, Task> eventAppearedAsync,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionMessage2(source, stream, settings, userCredentials,
                                                                  eventAppearedAsync, subscriptionDropped,
                                                                  _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }
        public Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings,
          Action<IHandlerRegistration> addHandlers,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == addHandlers) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.addHandlers); }

            var handlerCollection = new DefaultHandlerCollection();
            addHandlers(handlerCollection);
            Task LocalEventAppearedAsync(EventStoreSubscription sub, IResolvedEvent2 @event)
            {
                var handler = handlerCollection.GetHandler(@event.GetBody().GetType());
                return handler(@event);
            }
            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionMessage2(source, stream, settings, userCredentials,
                                                                  LocalEventAppearedAsync, subscriptionDropped,
                                                                  _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }

        #endregion

        #region -- VolatileSubscribeAsync<TEvent> --

        public Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(string topic, SubscriptionSettings settings,
          Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var stream = EventManager.GetStreamId<TEvent>(topic);
            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionMessageWrapper
            {
                Source = source,
                EventType = typeof(TEvent),
                MaxRetries = _settings.MaxRetries,
                Timeout = _settings.OperationTimeout,
                Message = new StartSubscriptionMessage<TEvent>(source, stream, settings, userCredentials,
                                                             eventAppeared, subscriptionDropped,
                                                             _settings.MaxRetries, _settings.OperationTimeout)
            });
            return source.Task;
        }
        public Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(string topic, SubscriptionSettings settings,
          Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var stream = EventManager.GetStreamId<TEvent>(topic);
            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionMessageWrapper
            {
                Source = source,
                EventType = typeof(TEvent),
                MaxRetries = _settings.MaxRetries,
                Timeout = _settings.OperationTimeout,
                Message = new StartSubscriptionMessage<TEvent>(source, stream, settings, userCredentials,
                                                             eventAppearedAsync, subscriptionDropped,
                                                             _settings.MaxRetries, _settings.OperationTimeout)
            });
            return source.Task;
        }

        #endregion

        #region -- SubscribeToStreamAsync  --

        public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings,
          Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionRawMessage(source, stream, settings, userCredentials,
                                                                    eventAppeared, subscriptionDropped,
                                                                    _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }
        public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings,
          Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionRawMessage(source, stream, settings, userCredentials,
                                                                    eventAppearedAsync, subscriptionDropped,
                                                                    _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }

        #endregion


        #region -- CatchUpSubscribe --

        public EventStoreCatchUpSubscription CatchUpSubscribe(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
          Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            var catchUpSubscription =
                    new EventStoreCatchUpSubscription(this, stream, lastCheckpoint,
                                                      userCredentials, eventAppeared, liveProcessingStarted,
                                                      subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }
        public EventStoreCatchUpSubscription CatchUpSubscribe(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
          Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            var catchUpSubscription =
                    new EventStoreCatchUpSubscription(this, stream, lastCheckpoint,
                                                      userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                      subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }
        public EventStoreCatchUpSubscription2 CatchUpSubscribe(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Action<IHandlerRegistration> addHandlers, Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
          Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == addHandlers) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.addHandlers); }

            var handlerCollection = new DefaultHandlerCollection();
            addHandlers(handlerCollection);
            Task LocalEventAppearedAsync(EventStoreCatchUpSubscription2 sub, IResolvedEvent2 @event)
            {
                var handler = handlerCollection.GetHandler(@event.GetBody().GetType());
                return handler(@event);
            }
            var catchUpSubscription =
                    new EventStoreCatchUpSubscription2(this, stream, lastCheckpoint,
                                                      userCredentials, LocalEventAppearedAsync, liveProcessingStarted,
                                                      subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        #endregion

        #region -- CatchUpSubscribe<TEvent> --

        public EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared, Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
          Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            var catchUpSubscription =
                    new EventStoreCatchUpSubscription<TEvent>(this, topic, lastCheckpoint,
                                                      userCredentials, eventAppeared, liveProcessingStarted,
                                                      subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        public EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
          Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            var catchUpSubscription =
                    new EventStoreCatchUpSubscription<TEvent>(this, topic, lastCheckpoint,
                                                      userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                      subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        #endregion

        #region -- SubscribeToStreamFrom --

        public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared, Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
          Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            var catchUpSubscription =
                    new EventStoreStreamCatchUpSubscription(this, stream, lastCheckpoint,
                                                            userCredentials, eventAppeared, liveProcessingStarted,
                                                            subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync, Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
          Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            var catchUpSubscription =
                    new EventStoreStreamCatchUpSubscription(this, stream, lastCheckpoint,
                                                            userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                            subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        #endregion


        #region -- SubscribeToAllAsync --

        public Task<EventStoreSubscription> SubscribeToAllAsync(SubscriptionSettings settings,
          Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionRawMessage(source, string.Empty, settings, userCredentials,
                                                                 eventAppeared, subscriptionDropped,
                                                                 _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }
        public Task<EventStoreSubscription> SubscribeToAllAsync(SubscriptionSettings settings,
          Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
          Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var source = TaskCompletionSourceFactory.Create<EventStoreSubscription>();
            _handler.EnqueueMessage(new StartSubscriptionRawMessage(source, string.Empty, settings, userCredentials,
                                                                 eventAppearedAsync, subscriptionDropped,
                                                                 _settings.MaxRetries, _settings.OperationTimeout));
            return source.Task;
        }

        #endregion

        #region -- SubscribeToAllFrom --

        public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Action<EventStoreAllCatchUpSubscription, ResolvedEvent> eventAppeared,
          Action<EventStoreAllCatchUpSubscription> liveProcessingStarted = null,
          Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            var catchUpSubscription =
                    new EventStoreAllCatchUpSubscription(this, lastCheckpoint,
                                                         userCredentials, eventAppeared, liveProcessingStarted,
                                                         subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
          Func<EventStoreAllCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
          Action<EventStoreAllCatchUpSubscription> liveProcessingStarted = null,
          Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            var catchUpSubscription =
                    new EventStoreAllCatchUpSubscription(this, lastCheckpoint,
                                                         userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                         subscriptionDropped, settings);
            catchUpSubscription.StartAsync();
            return catchUpSubscription;
        }

        #endregion


        #region -- PersistentSubscribeAsync --

        public Task<EventStorePersistentSubscription> PersistentSubscribeAsync(string stream, string subscriptionId,
          ConnectToPersistentSubscriptionSettings settings,
          Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
          Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(subscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var subscription = new EventStorePersistentSubscription(subscriptionId, stream, settings,
                eventAppeared, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }
        public Task<EventStorePersistentSubscription> PersistentSubscribeAsync(string stream, string subscriptionId,
          ConnectToPersistentSubscriptionSettings settings,
          Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
          Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(subscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var subscription = new EventStorePersistentSubscription(subscriptionId, stream, settings,
                eventAppearedAsync, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }
        public Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(string stream, string subscriptionId,
          ConnectToPersistentSubscriptionSettings settings, Action<IHandlerRegistration> addHandlers,
          Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(subscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == addHandlers) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.addHandlers); }

            var handlerCollection = new DefaultHandlerCollection();
            addHandlers(handlerCollection);
            Task LocalEventAppearedAsync(EventStorePersistentSubscription2 sub, IResolvedEvent2 @event, int? retryCount)
            {
                var handler = handlerCollection.GetHandler(@event.GetBody().GetType());
                return handler(@event);
            }
            var subscription = new EventStorePersistentSubscription2(subscriptionId, stream, settings,
                LocalEventAppearedAsync, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }

        #endregion

        #region -- PersistentSubscribeAsync<TEvent> --

        public Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(string topic, string subscriptionId,
          ConnectToPersistentSubscriptionSettings settings,
          Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
          Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(subscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var stream = EventManager.GetStreamId<TEvent>(topic);
            var subscription = new EventStorePersistentSubscription<TEvent>(subscriptionId, stream, settings,
                eventAppeared, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }
        public Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(string topic, string subscriptionId,
          ConnectToPersistentSubscriptionSettings settings,
          Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
          Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(subscriptionId)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.subscriptionId); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var stream = EventManager.GetStreamId<TEvent>(topic);
            var subscription = new EventStorePersistentSubscription<TEvent>(subscriptionId, stream, settings,
                eventAppearedAsync, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }

        #endregion

        #region -- ConnectToPersistentSubscriptionAsync --

        public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
          ConnectToPersistentSubscriptionSettings settings,
          Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
          Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(groupName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groupName); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppeared) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppeared); }

            var subscription = new EventStorePersistentRawSubscription(groupName, stream, settings,
                eventAppeared, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }
        public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
          ConnectToPersistentSubscriptionSettings settings,
          Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
          Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
          UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(groupName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groupName); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            if (null == eventAppearedAsync) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.eventAppearedAsync); }

            var subscription = new EventStorePersistentRawSubscription(groupName, stream, settings,
                eventAppearedAsync, subscriptionDropped, userCredentials, _settings, _handler);

            return subscription.StartAsync();
        }

        #endregion

        #region -- Create/Update/Delete PersistentSubscription --

        public async Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(groupName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groupName); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            var source = TaskCompletionSourceFactory.Create<PersistentSubscriptionCreateResult>();
            EnqueueOperation(new CreatePersistentSubscriptionOperation(source, stream, groupName, settings, userCredentials));
            await source.Task.ConfigureAwait(false);
        }

        public async Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(groupName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groupName); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            var source = TaskCompletionSourceFactory.Create<PersistentSubscriptionUpdateResult>();
            EnqueueOperation(new UpdatePersistentSubscriptionOperation(source, stream, groupName, settings, userCredentials));
            await source.Task.ConfigureAwait(false);
        }

        public async Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(groupName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groupName); }
            var source = TaskCompletionSourceFactory.Create<PersistentSubscriptionDeleteResult>();
            EnqueueOperation(new DeletePersistentSubscriptionOperation(source, stream, groupName, userCredentials));
            await source.Task.ConfigureAwait(false);
        }

        public Task CreatePersistentSubscriptionAsync<TEvent>(string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            var stream = EventManager.GetStreamId<TEvent>(topic);
            return CreatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
        }

        public Task UpdatePersistentSubscriptionAsync<TEvent>(string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            var stream = EventManager.GetStreamId<TEvent>(topic);
            return UpdatePersistentSubscriptionAsync(stream, groupName, settings, credentials);

        }

        public Task DeletePersistentSubscriptionAsync<TEvent>(string topic, string groupName, UserCredentials userCredentials = null)
        {
            var stream = EventManager.GetStreamId<TEvent>(topic);
            return DeletePersistentSubscriptionAsync(stream, groupName, userCredentials);
        }

        #endregion


        #region -- StreamMetadata --

        public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
        {
            return SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata.AsJsonBytes(), userCredentials);
        }

        public async Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (SystemStreams.IsMetastream(stream))
            {
                CoreThrowHelper.ThrowArgumentException_SettingMetadataForMetastreamIsNotSupported(stream);
            }

            var source = TaskCompletionSourceFactory.Create<WriteResult>();

            var metaevent = new EventData(Guid.NewGuid(), SystemEventTypes.StreamMetadata, true, metadata ?? Empty.ByteArray, null);
            EnqueueOperation(new AppendToStreamOperation(source,
                                                         _settings.RequireMaster,
                                                         SystemStreams.MetastreamOf(stream),
                                                         expectedMetastreamVersion,
                                                         new[] { metaevent },
                                                         userCredentials));
            return await source.Task.ConfigureAwait(false);
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
                        if (res.Event == null) CoreThrowHelper.ThrowException_EventIsNullWhileOperationResultIsSuccess();
                        var evnt = res.Event.Value.OriginalEvent;
                        if (evnt == null) return new RawStreamMetadataResult(stream, false, -1, Empty.ByteArray);
                        return new RawStreamMetadataResult(stream, false, evnt.EventNumber, evnt.Data);
                    case EventReadStatus.NotFound:
                    case EventReadStatus.NoStream:
                        return new RawStreamMetadataResult(stream, false, -1, Empty.ByteArray);
                    case EventReadStatus.StreamDeleted:
                        return new RawStreamMetadataResult(stream, true, long.MaxValue, Empty.ByteArray);
                    default:
                        CoreThrowHelper.ThrowArgumentOutOfRangeException_UnexpectedReadEventResult(res.Status); return default;
                }
            });
        }

        #endregion

        #region -- SetSystemSettingsAsync --

        public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null)
        {
            return AppendToStreamAsync(SystemStreams.SettingsStream, ExpectedVersion.Any,
                                       new EventData(Guid.NewGuid(), SystemEventTypes.Settings, true, settings.ToJsonBytes(), null),
                                       userCredentials);
        }

        #endregion

        #region -- Event handlers --

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

        #endregion
    }
}