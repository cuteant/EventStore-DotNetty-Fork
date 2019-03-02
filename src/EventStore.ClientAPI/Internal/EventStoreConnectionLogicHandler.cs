using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using CuteAnt.Reflection;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
    internal partial class EventStoreConnectionLogicHandler : IEventStoreConnectionLogicHandler, IConnectionEventHandler, IHasTcpPackageConnection
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<EventStoreConnectionLogicHandler>();
        private static readonly TimerTickMessage TimerTickMessage = new TimerTickMessage();

        public int TotalOperationCount { get { return _operations.TotalOperationCount; } }

        private readonly IEventStoreConnection2 _esConnection;
        private readonly ConnectionSettings _settings;
        private readonly bool _verboseDebug;
        private readonly bool _verboseInfo;
        private readonly byte ClientVersion = 1;

        private readonly SimpleQueuedHandler _queue = new SimpleQueuedHandler();
        private readonly Timer _timer;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly OperationsManager _operations;
        private readonly SubscriptionsManager _subscriptions;

        private IEndPointDiscoverer _endPointDiscoverer;

        private TimeSpan _lastTimeoutsTimeStamp;
        private int _packageNumber;
        private int _wasConnected;

        private TcpPackageConnection _connection;
        private TcpPackageConnection InternalConnection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _connection);
            set => Interlocked.Exchange(ref _connection, value);
        }
        TcpPackageConnection IHasTcpPackageConnection.Connection => Volatile.Read(ref _connection);

        private ReconnectionInfo _reconnInfo = ReconnectionInfo.Default;
        private ReconnectionInfo InternalReconnectionInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _reconnInfo);
            set => Interlocked.Exchange(ref _reconnInfo, value);
        }

        private HeartbeatInfo _heartbeatInfo = HeartbeatInfo.Default;
        private HeartbeatInfo InternalHeartbeatInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _heartbeatInfo);
            set => Interlocked.Exchange(ref _heartbeatInfo, value);
        }

        private AuthInfo _authInfo = AuthInfo.Default;
        private AuthInfo InternalAuthInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _authInfo);
            set => Interlocked.Exchange(ref _authInfo, value);
        }

        private IdentifyInfo _identifyInfo = IdentifyInfo.Default;
        private IdentifyInfo InternalIdentifyInfo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _identifyInfo);
            set => Interlocked.Exchange(ref _identifyInfo, value);
        }

        private int _state = InternalConnectionState.Init;
        internal int ConnectionState
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _state);
            set => Interlocked.Exchange(ref _state, value);
        }

        private int _connectingPhase = InternalConnectingPhase.Invalid;
        internal int ConnectingPhase
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _connectingPhase);
            set => Interlocked.Exchange(ref _connectingPhase, value);
        }

        public EventStoreConnectionLogicHandler(IEventStoreConnection2 esConnection, ConnectionSettings settings)
        {
            if (null == esConnection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.esConnection); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }

            _esConnection = esConnection;
            _settings = settings;
            _verboseDebug = _settings.VerboseLogging && s_logger.IsDebugLevelEnabled();
            _verboseInfo = _settings.VerboseLogging && s_logger.IsInformationLevelEnabled();

            _operations = new OperationsManager(_esConnection.ConnectionName, settings);
            _subscriptions = new SubscriptionsManager(_esConnection.ConnectionName, settings);

            _queue.RegisterHandler<StartConnectionMessage>(msg => StartConnectionAsync(msg.Task, msg.EndPointDiscoverer));
            _queue.RegisterHandler<CloseConnectionMessage>(msg => CloseConnectionAsync(msg.Reason, msg.Exception));

            _queue.RegisterHandler<StartOperationMessage>(msg => StartOperationAsync(msg.Operation, msg.MaxRetries, msg.Timeout));

            _queue.RegisterHandler<StartSubscriptionRawMessage>(StartSubscriptionAsync);
            _queue.RegisterHandler<StartSubscriptionMessage>(StartSubscriptionAsync);
            _queue.RegisterHandler<StartSubscriptionMessageWrapper>(StartSubscriptionAsync);
            _queue.RegisterHandler<StartSubscriptionMessage2>(StartSubscriptionAsync);

            _queue.RegisterHandler<StartPersistentSubscriptionRawMessage>(StartSubscriptionAsync);
            _queue.RegisterHandler<StartPersistentSubscriptionMessage>(StartSubscriptionAsync);
            _queue.RegisterHandler<StartPersistentSubscriptionMessageWrapper>(StartSubscriptionAsync);
            _queue.RegisterHandler<StartPersistentSubscriptionMessage2>(StartSubscriptionAsync);

            _queue.RegisterHandler<EstablishTcpConnectionMessage>(msg => EstablishTcpConnectionAsync(msg.EndPoints));
            _queue.RegisterHandler<TcpConnectionEstablishedMessage>(msg => TcpConnectionEstablishedAsync(msg.Connection));
            _queue.RegisterHandler<TcpConnectionErrorMessage>(msg => TcpConnectionErrorAsync(msg.Connection, msg.Exception));
            _queue.RegisterHandler<TcpConnectionClosedMessage>(msg => TcpConnectionClosedAsync(msg.Connection));
            _queue.RegisterHandler<HandleTcpPackageMessage>(msg => HandleTcpPackageAsync(msg.Connection, msg.Package));

            _queue.RegisterHandler<TimerTickMessage>(msg => TimerTickAsync());

            _queue.Build();

            _timer = new Timer(_ => EnqueueMessage(TimerTickMessage), null, Consts.TimerPeriod, Consts.TimerPeriod);
        }

        public void EnqueueMessage(Message message)
        {
            if (_verboseDebug && message != TimerTickMessage) { LogEnqueueingMessage(message); }
            _queue.EnqueueMessage(message);
        }

        public Task EnqueueMessageAsync(Message message)
        {
            if (_verboseDebug && message != TimerTickMessage) { LogEnqueueingMessage(message); }
            return _queue.EnqueueMessageAsync(message);
        }

        private async Task StartConnectionAsync(TaskCompletionSource<object> task, IEndPointDiscoverer endPointDiscoverer)
        {
            if (null == task) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.task); }
            if (null == endPointDiscoverer) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPointDiscoverer); }

            if (_verboseDebug) LogStartConnection();

            switch (ConnectionState)
            {
                case InternalConnectionState.Init:
                    Interlocked.Exchange(ref _endPointDiscoverer, endPointDiscoverer);
                    ConnectionState = InternalConnectionState.Connecting;
                    ConnectingPhase = InternalConnectingPhase.Reconnecting;
                    DiscoverEndPoint(task);
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    task.SetException(ExConnectionAlreadyActive());
                    break;
                case InternalConnectionState.Closed:
                    task.SetException(new ObjectDisposedException(_esConnection.ConnectionName));
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(ConnectionState); break;
            }
        }

        private void DiscoverEndPoint(TaskCompletionSource<object> completionTask)
        {
            if (_verboseDebug) LogDiscoverEndPoint();

            if (ConnectionState != InternalConnectionState.Connecting) return;
            if (ConnectingPhase != InternalConnectingPhase.Reconnecting) return;

            ConnectingPhase = InternalConnectingPhase.EndPointDiscovery;

            var endPointDiscoverer = Volatile.Read(ref _endPointDiscoverer);
            endPointDiscoverer.DiscoverAsync(InternalConnection?.RemoteEndPoint).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    EnqueueMessage(CloseConnectionMessage.Create_FailedToResolveTcpEndpointToWhichToConnect(t.Exception));
                    completionTask?.SetException(CoreThrowHelper.GetCannotEstablishConnectionException(t.Exception));
                }
                else
                {
                    EnqueueMessage(new EstablishTcpConnectionMessage(t.Result));
                    completionTask?.SetResult(null);
                }
            });
        }

        private async Task EstablishTcpConnectionAsync(NodeEndPoints endPoints)
        {
            var endPoint = _settings.UseSslConnection ? endPoints.SecureTcpEndPoint ?? endPoints.TcpEndPoint : endPoints.TcpEndPoint;
            if (endPoint == null)
            {
                await CloseConnectionAsync("No end point to node specified.").ConfigureAwait(false);
                return;
            }

            if (_verboseDebug) LogEstablishTcpConnectionTo(endPoint);

            if (ConnectionState != InternalConnectionState.Connecting) return;
            if (ConnectingPhase != InternalConnectingPhase.EndPointDiscovery) return;

            var settings = new DotNettyTransportSettings(
                 enableLibuv: _settings.EnableLibuv,
                 connectTimeout: _settings.ClientConnectionTimeout,
                 serverSocketWorkerPoolSize: 2,
                 clientSocketWorkerPoolSize: ScaledPoolSize(_settings.SocketWorkerPoolSizeMin, _settings.SocketWorkerPoolSizeFactor, _settings.SocketWorkerPoolSizeMax),
                 maxFrameSize: int.MaxValue,
                 dnsUseIpv6: false,
                 tcpReuseAddr: true,
                 tcpReusePort: true,
                 tcpKeepAlive: true,
                 tcpNoDelay: true,
                 tcpLinger: 0,
                 backlog: 200,
                 enforceIpFamily: false,
                 receiveBufferSize: _settings.ReceiveBufferSize,
                 sendBufferSize: _settings.SendBufferSize,
                 writeBufferHighWaterMark: _settings.WriteBufferHighWaterMark,
                 writeBufferLowWaterMark: _settings.WriteBufferLowWaterMark,
                 enableBufferPooling: _settings.EnableBufferPooling);

            ConnectingPhase = InternalConnectingPhase.ConnectionEstablishing;
            var connection = new TcpPackageConnection(
                    settings,
                    endPoint,
                    _settings.UseSslConnection,
                    _settings.TargetHost,
                    _settings.ValidateServer,
                    this);
            InternalConnection = connection;
            connection.ConnectAsync().Ignore();
        }
        private static int ScaledPoolSize(int floor, double scalar, int ceiling)
        {
            return Math.Min(Math.Max((int)(Environment.ProcessorCount * scalar), floor), ceiling);
        }

        void IConnectionEventHandler.Handle(TcpPackageConnection connection, TcpPackage package)
        {
            EnqueueMessage(new HandleTcpPackageMessage(connection, package));
        }

        void IConnectionEventHandler.OnConnectionEstablished(TcpPackageConnection connection)
        {
            EnqueueMessage(new TcpConnectionEstablishedMessage(connection));
        }

        void IConnectionEventHandler.OnError(TcpPackageConnection connection, Exception exc)
        {
            EnqueueMessage(new TcpConnectionErrorMessage(connection, exc));
        }

        void IConnectionEventHandler.OnConnectionClosed(TcpPackageConnection connection, DisassociateInfo disassociateInfo)
        {
            EnqueueMessage(new TcpConnectionClosedMessage(connection, disassociateInfo));
        }

        private async Task TcpConnectionErrorAsync(TcpPackageConnection connection, Exception exception)
        {
            if (InternalConnection != connection) return;
            if (ConnectionState == InternalConnectionState.Closed) return;

            if (_verboseDebug) LogTcpConnectionError(connection.ConnectionId, exception);
            await CloseConnectionAsync("TCP connection error occurred.", exception).ConfigureAwait(false);
        }

        private async Task CloseConnectionAsync(string reason, Exception exception = null)
        {
            if (ConnectionState == InternalConnectionState.Closed)
            {
                await TaskConstants.Completed;
                if (_verboseDebug) LogCloseConnectionIgnoredBecauseIsESConnectionIsClosed(reason, exception);
                return;
            }

            if (_verboseDebug) LogCloseConnectionReason(reason, exception);

            ConnectionState = InternalConnectionState.Closed;

            _timer.Dispose();
            _operations.CleanUp();
            _subscriptions.CleanUp();
            await CloseTcpConnection(reason).ConfigureAwait(false);

            if (_verboseInfo) LogClosedReason(reason);

            if (exception != null) { RaiseErrorOccurred(exception); }

            RaiseClosed(reason);
        }

        private async Task CloseTcpConnection(string reason)
        {
            var innerConn = InternalConnection;
            if (innerConn == null)
            {
                if (_verboseDebug) LogCloseTcpConnectionIgnoredBecauseConnectionIsNull();
                return;
            }

            if (_verboseDebug) LogCloseTcpConnection();
            innerConn.Close(reason);
            await TcpConnectionClosedAsync(innerConn).ConfigureAwait(false);
            InternalConnection = null;
        }

        private async Task TcpConnectionClosedAsync(TcpPackageConnection conn)
        {
            var state = ConnectionState;
            if (state == InternalConnectionState.Init)
            {
                await TaskConstants.Completed;
                CoreThrowHelper.ThrowException();
            }
            var innerConn = InternalConnection;
            if (state == InternalConnectionState.Closed || innerConn != conn)
            {
                if (_verboseDebug) LogIgnoredBecauseTcpConnectionClosed(conn);
                return;
            }

            ConnectionState = InternalConnectionState.Connecting;
            ConnectingPhase = InternalConnectingPhase.Reconnecting;

            if (_verboseDebug) LogTcpConnectionClosed(conn);

            _subscriptions.PurgeSubscribedAndDroppedSubscriptions(innerConn.ConnectionId);
            var reconnInfo = InternalReconnectionInfo;
            InternalReconnectionInfo = new ReconnectionInfo(reconnInfo.ReconnectionAttempt, _stopwatch.Elapsed);

            if (Interlocked.CompareExchange(ref _wasConnected, 0, 1) == 1)
            {
                RaiseDisconnected(conn.RemoteEndPoint);
            }
        }

        private async Task TcpConnectionEstablishedAsync(TcpPackageConnection conn)
        {
            var innerConn = InternalConnection;
            if (ConnectionState != InternalConnectionState.Connecting || innerConn != conn || conn.IsClosed)
            {
                await TaskConstants.Completed;
                if (_verboseDebug) LogIgnoredBecauseTcpConnectionEstablished(conn);
                return;
            }

            if (_verboseDebug) LogTcpConnectionEstablished(conn);
            InternalHeartbeatInfo = new HeartbeatInfo(Volatile.Read(ref _packageNumber), true, _stopwatch.Elapsed);

            if (_settings.DefaultUserCredentials != null)
            {
                ConnectingPhase = InternalConnectingPhase.Authentication;

                var authInfo = new AuthInfo(Guid.NewGuid(), _stopwatch.Elapsed);
                InternalAuthInfo = authInfo;
                innerConn.EnqueueSend(new TcpPackage(TcpCommand.Authenticate,
                                                    TcpFlags.Authenticated,
                                                    authInfo.CorrelationId,
                                                    _settings.DefaultUserCredentials.Username,
                                                    _settings.DefaultUserCredentials.Password,
                                                    null));
            }
            else
            {
                GoToIdentifyState();
            }
        }

        private void GoToIdentifyState()
        {
            var innerConn = InternalConnection;
            if (null == innerConn) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument._connection); }
            ConnectingPhase = InternalConnectingPhase.Identification;

            var identifyInfo = new IdentifyInfo(Guid.NewGuid(), _stopwatch.Elapsed);
            InternalIdentifyInfo = identifyInfo;
            var dto = new TcpClientMessageDto.IdentifyClient(ClientVersion, _esConnection.ConnectionName);
            innerConn.EnqueueSend(new TcpPackage(TcpCommand.IdentifyClient, identifyInfo.CorrelationId, dto.Serialize()));
        }

        private void GoToConnectedState()
        {
            var innerConn = InternalConnection;
            if (null == innerConn) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument._connection); }

            ConnectionState = InternalConnectionState.Connected;
            ConnectingPhase = InternalConnectingPhase.Connected;

            Interlocked.CompareExchange(ref _wasConnected, 1, 0);

            RaiseConnectedEvent(innerConn.RemoteEndPoint);

            if (_stopwatch.Elapsed - _lastTimeoutsTimeStamp >= _settings.OperationTimeoutCheckPeriod)
            {
                _operations.CheckTimeoutsAndRetry(innerConn);
                _subscriptions.CheckTimeoutsAndRetry(innerConn);
                _lastTimeoutsTimeStamp = _stopwatch.Elapsed;
            }
        }

        private async Task TimerTickAsync()
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init: break;
                case InternalConnectionState.Connecting:
                    var reconnInfo = InternalReconnectionInfo;
                    var connectingPhase = ConnectingPhase;
                    switch (connectingPhase)
                    {
                        case InternalConnectingPhase.Reconnecting when _stopwatch.Elapsed - reconnInfo.TimeStamp >= _settings.ReconnectionDelay:
                            if (_verboseDebug) LogTimerTickCheckingReconnection();

                            InternalReconnectionInfo = reconnInfo = new ReconnectionInfo(reconnInfo.ReconnectionAttempt + 1, _stopwatch.Elapsed);
                            if (_settings.MaxReconnections >= 0 && reconnInfo.ReconnectionAttempt > _settings.MaxReconnections)
                            {
                                await CloseConnectionAsync("Reconnection limit reached.").ConfigureAwait(false);
                            }
                            else
                            {
                                RaiseReconnecting();
                                // Tcp Connection 未真正建立，先传递空Connection
                                _operations.CheckTimeoutsAndRetry(null); // InternalConnection
                                DiscoverEndPoint(null);
                            }
                            break;

                        case InternalConnectingPhase.Authentication when _stopwatch.Elapsed - InternalAuthInfo.TimeStamp >= _settings.OperationTimeout:
                            RaiseAuthenticationFailed("Authentication timed out.");
                            GoToIdentifyState();
                            break;

                        case InternalConnectingPhase.Identification when _stopwatch.Elapsed - InternalIdentifyInfo.TimeStamp >= _settings.OperationTimeout:
                            const string msg = "Timed out waiting for client to be identified";
                            if (_verboseDebug) LogTimedoutWaitingForClientToBeIdentified();
                            await CloseTcpConnection(msg).ConfigureAwait(false);
                            break;
                    }
                    if (connectingPhase > InternalConnectingPhase.ConnectionEstablishing)
                    {
                        await ManageHeartbeatsAsync().ConfigureAwait(false);
                    }
                    break;

                case InternalConnectionState.Connected:
                    // operations timeouts are checked only if connection is established and check period time passed
                    if (_stopwatch.Elapsed - _lastTimeoutsTimeStamp >= _settings.OperationTimeoutCheckPeriod)
                    {
                        // On mono even impossible connection first says that it is established
                        // so clearing of reconnection count on ConnectionEstablished event causes infinite reconnections.
                        // So we reset reconnection count to zero on each timeout check period when connection is established
                        InternalReconnectionInfo = new ReconnectionInfo(0, _stopwatch.Elapsed);
                        var innerConn = InternalConnection;
                        _operations.CheckTimeoutsAndRetry(innerConn);
                        _subscriptions.CheckTimeoutsAndRetry(innerConn);
                        _lastTimeoutsTimeStamp = _stopwatch.Elapsed;
                    }
                    await ManageHeartbeatsAsync().ConfigureAwait(false);
                    break;

                case InternalConnectionState.Closed: break;
                default: ThrowException(state); break;
            }
        }

        private async Task ManageHeartbeatsAsync()
        {
            var innerConn = InternalConnection;
            if (innerConn == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument._connection); }

            var heartbeatInfo = InternalHeartbeatInfo;
            var timeout = heartbeatInfo.IsIntervalStage ? _settings.HeartbeatInterval : _settings.HeartbeatTimeout;
            if (_stopwatch.Elapsed - heartbeatInfo.TimeStamp < timeout) { return; }

            var packageNumber = Volatile.Read(ref _packageNumber);
            if (heartbeatInfo.LastPackageNumber != packageNumber)
            {
                InternalHeartbeatInfo = new HeartbeatInfo(packageNumber, true, _stopwatch.Elapsed);
                return;
            }

            if (heartbeatInfo.IsIntervalStage)
            {
                // TcpMessage.Heartbeat analog
                innerConn.EnqueueSend(new TcpPackage(TcpCommand.HeartbeatRequestCommand, Guid.NewGuid(), null));
                InternalHeartbeatInfo = new HeartbeatInfo(heartbeatInfo.LastPackageNumber, false, _stopwatch.Elapsed);
                return;
            }
            // TcpMessage.HeartbeatTimeout analog
            await CloseTcpConnection(packageNumber).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task CloseTcpConnection(int packageNumber)
        {
            var innerConn = InternalConnection;
            // TcpMessage.HeartbeatTimeout analog
            var msg = string.Format("EventStoreConnection '{0}': closing TCP connection [{1}, {2}, {3}] due to HEARTBEAT TIMEOUT at pkgNum {4}.",
                                    _esConnection.ConnectionName, innerConn.RemoteEndPoint, innerConn.LocalEndPoint,
                                    innerConn.ConnectionId, packageNumber);
            if (s_logger.IsInformationLevelEnabled()) { s_logger.LogInformation(msg); }
            await CloseTcpConnection(msg).ConfigureAwait(false);
        }

        private async Task StartOperationAsync(IClientOperation operation, int maxRetries, TimeSpan timeout)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    operation.Fail(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                    if (_verboseDebug) LogStartOperationEnqueue(operation, maxRetries, timeout);
                    _operations.EnqueueOperation(new OperationItem(operation, maxRetries, timeout));
                    break;
                case InternalConnectionState.Connected:
                    if (_verboseDebug) LogStartOperationSchedule(operation, maxRetries, timeout);
                    _operations.ScheduleOperation(new OperationItem(operation, maxRetries, timeout), InternalConnection);
                    break;
                case InternalConnectionState.Closed:
                    operation.Fail(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionMessageWrapper msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var volatileSubscriptionOperationWrapperType = typeof(SubscriptionOperationWrapper<>).GetCachedGenericType(msg.EventType);
                    var volatileSubscriptionOperationWrapper = ActivatorUtils.FastCreateInstance<IVolatileSubscriptionOperationWrapper>(volatileSubscriptionOperationWrapperType);
                    var operation = volatileSubscriptionOperationWrapper.Create(msg, this, _esConnection.EventAdapter);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionMessage msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var operation = msg.EventAppeared != null
                                  ? new SubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                              msg.EventAppeared, msg.SubscriptionDropped, this, _esConnection.EventAdapter)
                                  : new SubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                              msg.EventAppearedAsync, msg.SubscriptionDropped, this, _esConnection.EventAdapter);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionMessage2 msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var operation = msg.EventAppeared != null
                                  ? new SubscriptionOperation2(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                               msg.EventAppeared, msg.SubscriptionDropped, this, _esConnection.EventAdapter)
                                  : new SubscriptionOperation2(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                               msg.EventAppearedAsync, msg.SubscriptionDropped, this, _esConnection.EventAdapter);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionRawMessage msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var operation = msg.EventAppeared != null
                                  ? new VolatileSubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                      msg.EventAppeared, msg.SubscriptionDropped, this)
                                  : new VolatileSubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                      msg.EventAppearedAsync, msg.SubscriptionDropped, this);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }

        private async Task StartSubscriptionAsync(StartPersistentSubscriptionMessageWrapper msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var persistentSubscriptionOperationWrapperType = typeof(PersistentSubscriptionOperationWrapper<>).GetCachedGenericType(msg.EventType);
                    var persistentSubscriptionOperationWrapper = ActivatorUtils.FastCreateInstance<IPersistentSubscriptionOperationWrapper>(persistentSubscriptionOperationWrapperType);
                    var operation = persistentSubscriptionOperationWrapper.Create(msg, this, _esConnection.EventAdapter);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartPersistentSubscriptionMessage msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var operation = new PersistentSubscriptionOperation(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                        msg.EventAppearedAsync, msg.SubscriptionDropped, this, _esConnection.EventAdapter);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartPersistentSubscriptionMessage2 msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var operation = new PersistentSubscriptionOperation2(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                         msg.EventAppearedAsync, msg.SubscriptionDropped, this, _esConnection.EventAdapter);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartPersistentSubscriptionRawMessage msg)
        {
            var state = ConnectionState;
            switch (state)
            {
                case InternalConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case InternalConnectionState.Connecting:
                case InternalConnectionState.Connected:
                    var innerConn = InternalConnection;
                    var operation = new ConnectToPersistentSubscriptionOperation(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                                 msg.EventAppearedAsync, msg.SubscriptionDropped, this);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (state == InternalConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, innerConn);
                    }
                    break;
                case InternalConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(state); break;
            }
        }

        private async Task HandleTcpPackageAsync(TcpPackageConnection conn, TcpPackage package)
        {
            var state = ConnectionState;
            var innerConn = InternalConnection;
            if (innerConn != conn || state == InternalConnectionState.Closed || state == InternalConnectionState.Init)
            {
                if (_verboseDebug) LogIgnoredTcpPackage(conn.ConnectionId, package.Command, package.CorrelationId);
                return;
            }

            if (_verboseDebug) LogHandleTcpPackage(innerConn.ConnectionId, package.Command, package.CorrelationId);
            Interlocked.Increment(ref _packageNumber);

            switch (package.Command)
            {
                case TcpCommand.HeartbeatResponseCommand: return;

                case TcpCommand.HeartbeatRequestCommand:
                    innerConn.EnqueueSend(new TcpPackage(TcpCommand.HeartbeatResponseCommand, package.CorrelationId, null));
                    return;

                case TcpCommand.Authenticated:
                case TcpCommand.NotAuthenticated:
                    if (state == InternalConnectionState.Connecting
                        && ConnectingPhase == InternalConnectingPhase.Authentication
                        && InternalAuthInfo.CorrelationId == package.CorrelationId)
                    {
                        if (package.Command == TcpCommand.NotAuthenticated)
                        {
                            RaiseAuthenticationFailed("Not authenticated");
                        }

                        GoToIdentifyState();
                        return;
                    }
                    break;

                case TcpCommand.ClientIdentified:
                    if (state == InternalConnectionState.Connecting && InternalIdentifyInfo.CorrelationId == package.CorrelationId)
                    {
                        GoToConnectedState();
                        return;
                    }
                    break;
                case TcpCommand.BadRequest when package.CorrelationId == Guid.Empty:
                    const string _closeReason = "Connection-wide BadRequest received. Too dangerous to continue.";
                    var exc = CoreThrowHelper.GetEventStoreConnectionException(package);
                    await CloseConnectionAsync(_closeReason, exc).ConfigureAwait(false);
                    return;
            }

            if (_operations.TryGetActiveOperation(package.CorrelationId, out OperationItem operation))
            {
                var result = operation.Operation.InspectPackage(package);
                if (_verboseDebug) LogHandleTcpPackageOPERATIONDECISION(result, operation);
                switch (result.Decision)
                {
                    case InspectionDecision.DoNothing: break;
                    case InspectionDecision.EndOperation:
                        _operations.RemoveOperation(operation);
                        break;
                    case InspectionDecision.Retry:
                        _operations.ScheduleOperationRetry(operation);
                        break;
                    case InspectionDecision.Reconnect:
                        await ReconnectToAsync(new NodeEndPoints(result.TcpEndPoint, result.SecureTcpEndPoint)).ConfigureAwait(false);
                        _operations.ScheduleOperationRetry(operation);
                        break;
                    default: ThrowException(result.Decision); break;
                }
                if (state == InternalConnectionState.Connected)
                {
                    _operations.TryScheduleWaitingOperations(conn);
                }
            }
            else if (_subscriptions.TryGetActiveSubscription(package.CorrelationId, out SubscriptionItem subscription))
            {
                var result = await subscription.Operation.InspectPackageAsync(package);
                if (_verboseDebug) LogHandleTcpPackageSUBSCRIPTIONDECISION(result, subscription);
                switch (result.Decision)
                {
                    case InspectionDecision.DoNothing: break;
                    case InspectionDecision.EndOperation:
                        _subscriptions.RemoveSubscription(subscription);
                        break;
                    case InspectionDecision.Retry:
                        _subscriptions.ScheduleSubscriptionRetry(subscription);
                        break;
                    case InspectionDecision.Reconnect:
                        await ReconnectToAsync(new NodeEndPoints(result.TcpEndPoint, result.SecureTcpEndPoint)).ConfigureAwait(false);
                        _subscriptions.ScheduleSubscriptionRetry(subscription);
                        break;
                    case InspectionDecision.Subscribed:
                        subscription.IsSubscribed = true;
                        break;
                    default: ThrowException(result.Decision); break;
                }
            }
            else
            {
                if (_verboseDebug) LogHandleTcpPackageUNMAPPEDPACKAGE(package.CorrelationId, package.Command);
            }
        }

        private async Task ReconnectToAsync(NodeEndPoints endPoints)
        {
            var endPoint = _settings.UseSslConnection
                          ? endPoints.SecureTcpEndPoint ?? endPoints.TcpEndPoint
                          : endPoints.TcpEndPoint;
            if (endPoint == null)
            {
                await CloseConnectionAsync("No end point is specified while trying to reconnect.").ConfigureAwait(false);
                return;
            }

            var innerConn = InternalConnection;
            if (ConnectionState != InternalConnectionState.Connected || innerConn.RemoteEndPoint.Equals(endPoint)) { return; }

            var msg = $"EventStoreConnection '{_esConnection.ConnectionName}': going to reconnect to [{endPoint}]. Current endpoint: [{innerConn.RemoteEndPoint}, L{innerConn.LocalEndPoint}].";
            if (_verboseInfo) { s_logger.LogInformation(msg); }
            await CloseTcpConnection(msg).ConfigureAwait(false);

            ConnectionState = InternalConnectionState.Connecting;
            ConnectingPhase = InternalConnectingPhase.EndPointDiscovery;
            await EstablishTcpConnectionAsync(endPoints).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectDisposedException EsConnectionDisposed()
        {
            return new ObjectDisposedException(_esConnection.ConnectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InvalidOperationException ExConnectionAlreadyActive()
        {
            return new InvalidOperationException($"EventStoreConnection '{_esConnection.ConnectionName}' is already active.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private InvalidOperationException ExConnectionIsNotActive()
        {
            return new InvalidOperationException($"EventStoreConnection '{_esConnection.ConnectionName}' is not active.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowException(int state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unknown state: {s_connectionStateInfo[state]}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowException(InspectionDecision decision)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unknown InspectionDecision: {decision}");
            }
        }

        private void RaiseConnectedEvent(IPEndPoint remoteEndPoint)
        {
            Connected(_esConnection, new ClientConnectionEventArgs(_esConnection, remoteEndPoint));
        }

        private void RaiseDisconnected(IPEndPoint remoteEndPoint)
        {
            Disconnected(_esConnection, new ClientConnectionEventArgs(_esConnection, remoteEndPoint));
        }

        private void RaiseClosed(string reason)
        {
            Closed(_esConnection, new ClientClosedEventArgs(_esConnection, reason));
        }

        private void RaiseErrorOccurred(Exception exception)
        {
            ErrorOccurred(_esConnection, new ClientErrorEventArgs(_esConnection, exception));
        }

        private void RaiseReconnecting()
        {
            Reconnecting(_esConnection, new ClientReconnectingEventArgs(_esConnection));
        }

        private void RaiseAuthenticationFailed(string reason)
        {
            AuthenticationFailed(_esConnection, new ClientAuthenticationFailedEventArgs(_esConnection, reason));
        }

        public event EventHandler<ClientConnectionEventArgs> Connected = delegate { };
        public event EventHandler<ClientConnectionEventArgs> Disconnected = delegate { };
        public event EventHandler<ClientReconnectingEventArgs> Reconnecting = delegate { };
        public event EventHandler<ClientClosedEventArgs> Closed = delegate { };
        public event EventHandler<ClientErrorEventArgs> ErrorOccurred = delegate { };
        public event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed = delegate { };

        private sealed class HeartbeatInfo
        {
            public static readonly HeartbeatInfo Default = new HeartbeatInfo();

            public readonly int LastPackageNumber;
            public readonly bool IsIntervalStage;
            public readonly TimeSpan TimeStamp;

            private HeartbeatInfo() { }
            public HeartbeatInfo(int lastPackageNumber, bool isIntervalStage, TimeSpan timeStamp)
            {
                LastPackageNumber = lastPackageNumber;
                IsIntervalStage = isIntervalStage;
                TimeStamp = timeStamp;
            }
        }

        private sealed class ReconnectionInfo
        {
            public static readonly ReconnectionInfo Default = new ReconnectionInfo();

            public readonly int ReconnectionAttempt;
            public readonly TimeSpan TimeStamp;

            private ReconnectionInfo() { }
            public ReconnectionInfo(int reconnectionAttempt, TimeSpan timeStamp)
            {
                ReconnectionAttempt = reconnectionAttempt;
                TimeStamp = timeStamp;
            }
        }

        private sealed class AuthInfo
        {
            public static readonly AuthInfo Default = new AuthInfo();

            public readonly Guid CorrelationId;
            public readonly TimeSpan TimeStamp;

            private AuthInfo() { }
            public AuthInfo(Guid correlationId, TimeSpan timeStamp)
            {
                CorrelationId = correlationId;
                TimeStamp = timeStamp;
            }
        }

        private sealed class IdentifyInfo
        {
            public static readonly IdentifyInfo Default = new IdentifyInfo();

            public readonly Guid CorrelationId;
            public readonly TimeSpan TimeStamp;

            private IdentifyInfo() { }
            public IdentifyInfo(Guid correlationId, TimeSpan timeStamp)
            {
                CorrelationId = correlationId;
                TimeStamp = timeStamp;
            }
        }

        private static readonly Dictionary<int, string> s_connectionStateInfo = new Dictionary<int, string>
        {
            { InternalConnectionState.Init, "Init" },
            { InternalConnectionState.Connecting, "Connecting" },
            { InternalConnectionState.Connected, "Connected" },
            { InternalConnectionState.Closed, "Closed" },
        };
        private static class InternalConnectionState
        {
            public const int Init = 0;
            public const int Connecting = 1;
            public const int Connected = 2;
            public const int Closed = 3;
        }

        private static class InternalConnectingPhase
        {
            public const int Invalid = 0;
            public const int Reconnecting = 1;
            public const int EndPointDiscovery = 2;
            public const int ConnectionEstablishing = 3;
            public const int Authentication = 4;
            public const int Identification = 5;
            public const int Connected = 6;
        }
    }
}
