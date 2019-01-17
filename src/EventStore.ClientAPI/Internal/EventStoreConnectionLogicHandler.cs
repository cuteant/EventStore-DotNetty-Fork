using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using CuteAnt.Reflection;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.Transport.Tcp;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
    internal partial class EventStoreConnectionLogicHandler : IEventStoreConnectionLogicHandler, IConnectionEventHandler
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<EventStoreConnectionLogicHandler>();
        private static readonly TimerTickMessage TimerTickMessage = new TimerTickMessage();

        public int TotalOperationCount { get { return _operations.TotalOperationCount; } }

        private readonly IEventStoreConnection _esConnection;
        private readonly ConnectionSettings _settings;
        private readonly bool _verboseDebug;
        private readonly bool _verboseInfo;
        private readonly byte ClientVersion = 1;

        private readonly SimpleQueuedHandler _queue = new SimpleQueuedHandler();
        private readonly Timer _timer;
        private IEndPointDiscoverer _endPointDiscoverer;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private ReconnectionInfo _reconnInfo;
        private HeartbeatInfo _heartbeatInfo;
        private AuthInfo _authInfo;
        private IdentifyInfo _identifyInfo;
        private TimeSpan _lastTimeoutsTimeStamp;
        private readonly OperationsManager _operations;
        private readonly SubscriptionsManager _subscriptions;

        private ConnectionState _state = ConnectionState.Init;
        private ConnectingPhase _connectingPhase = ConnectingPhase.Invalid;
        private int _wasConnected;

        private int _packageNumber;
        private TcpPackageConnection _connection;

        public EventStoreConnectionLogicHandler(IEventStoreConnection esConnection, ConnectionSettings settings)
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

            switch (_state)
            {
                case ConnectionState.Init:
                    _endPointDiscoverer = endPointDiscoverer;
                    _state = ConnectionState.Connecting;
                    _connectingPhase = ConnectingPhase.Reconnecting;
                    DiscoverEndPoint(task);
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    task.SetException(ExConnectionAlreadyActive());
                    break;
                case ConnectionState.Closed:
                    task.SetException(new ObjectDisposedException(_esConnection.ConnectionName));
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }

        private void DiscoverEndPoint(TaskCompletionSource<object> completionTask)
        {
            if (_verboseDebug) LogDiscoverEndPoint();

            if (_state != ConnectionState.Connecting) return;
            if (_connectingPhase != ConnectingPhase.Reconnecting) return;

            _connectingPhase = ConnectingPhase.EndPointDiscovery;

            _endPointDiscoverer.DiscoverAsync(_connection?.RemoteEndPoint).ContinueWith(t =>
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

            if (_state != ConnectionState.Connecting) return;
            if (_connectingPhase != ConnectingPhase.EndPointDiscovery) return;

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

            _connectingPhase = ConnectingPhase.ConnectionEstablishing;
            _connection = new TcpPackageConnection(
                    settings,
                    endPoint,
                    _settings.UseSslConnection,
                    _settings.TargetHost,
                    _settings.ValidateServer,
                    this);
            _connection.ConnectAsync().Ignore();
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
            if (_connection != connection) return;
            if (_state == ConnectionState.Closed) return;

            if (_verboseDebug) LogTcpConnectionError(connection.ConnectionId, exception);
            await CloseConnectionAsync("TCP connection error occurred.", exception).ConfigureAwait(false);
        }

        private async Task CloseConnectionAsync(string reason, Exception exception = null)
        {
            if (_state == ConnectionState.Closed)
            {
                await TaskConstants.Completed;
                if (_verboseDebug) LogCloseConnectionIgnoredBecauseIsESConnectionIsClosed(reason, exception);
                return;
            }

            if (_verboseDebug) LogCloseConnectionReason(reason, exception);

            _state = ConnectionState.Closed;

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
            if (_connection == null)
            {
                if (_verboseDebug) LogCloseTcpConnectionIgnoredBecauseConnectionIsNull();
                return;
            }

            if (_verboseDebug) LogCloseTcpConnection();
            _connection.Close(reason);
            await TcpConnectionClosedAsync(_connection).ConfigureAwait(false);
            _connection = null;
        }

        private async Task TcpConnectionClosedAsync(TcpPackageConnection connection)
        {
            if (_state == ConnectionState.Init)
            {
                await TaskConstants.Completed;
                CoreThrowHelper.ThrowException();
            }
            if (_state == ConnectionState.Closed || _connection != connection)
            {
                if (_verboseDebug) LogIgnoredBecauseTcpConnectionClosed(connection);
                return;
            }

            _state = ConnectionState.Connecting;
            _connectingPhase = ConnectingPhase.Reconnecting;

            if (_verboseDebug) LogTcpConnectionClosed(connection);

            _subscriptions.PurgeSubscribedAndDroppedSubscriptions(_connection.ConnectionId);
            _reconnInfo = new ReconnectionInfo(_reconnInfo.ReconnectionAttempt, _stopwatch.Elapsed);

            if (Interlocked.CompareExchange(ref _wasConnected, 0, 1) == 1)
            {
                RaiseDisconnected(connection.RemoteEndPoint);
            }
        }

        private async Task TcpConnectionEstablishedAsync(TcpPackageConnection connection)
        {
            if (_state != ConnectionState.Connecting || _connection != connection || connection.IsClosed)
            {
                await TaskConstants.Completed;
                if (_verboseDebug) LogIgnoredBecauseTcpConnectionEstablished(connection);
                return;
            }

            if (_verboseDebug) LogTcpConnectionEstablished(connection);
            _heartbeatInfo = new HeartbeatInfo(_packageNumber, true, _stopwatch.Elapsed);

            if (_settings.DefaultUserCredentials != null)
            {
                _connectingPhase = ConnectingPhase.Authentication;

                _authInfo = new AuthInfo(Guid.NewGuid(), _stopwatch.Elapsed);
                _connection.EnqueueSend(new TcpPackage(TcpCommand.Authenticate,
                                                       TcpFlags.Authenticated,
                                                       _authInfo.CorrelationId,
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
            if (null == _connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument._connection); }
            _connectingPhase = ConnectingPhase.Identification;

            _identifyInfo = new IdentifyInfo(Guid.NewGuid(), _stopwatch.Elapsed);
            var dto = new TcpClientMessageDto.IdentifyClient(ClientVersion, _esConnection.ConnectionName);
            _connection.EnqueueSend(new TcpPackage(TcpCommand.IdentifyClient, _identifyInfo.CorrelationId, dto.Serialize()));
        }

        private void GoToConnectedState()
        {
            if (null == _connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument._connection); }

            _state = ConnectionState.Connected;
            _connectingPhase = ConnectingPhase.Connected;

            Interlocked.CompareExchange(ref _wasConnected, 1, 0);

            RaiseConnectedEvent(_connection.RemoteEndPoint);

            if (_stopwatch.Elapsed - _lastTimeoutsTimeStamp >= _settings.OperationTimeoutCheckPeriod)
            {
                _operations.CheckTimeoutsAndRetry(_connection);
                _subscriptions.CheckTimeoutsAndRetry(_connection);
                _lastTimeoutsTimeStamp = _stopwatch.Elapsed;
            }
        }

        private async Task TimerTickAsync()
        {
            switch (_state)
            {
                case ConnectionState.Init: break;
                case ConnectionState.Connecting:
                    {
                        if (_connectingPhase == ConnectingPhase.Reconnecting && _stopwatch.Elapsed - _reconnInfo.TimeStamp >= _settings.ReconnectionDelay)
                        {
                            if (_verboseDebug) LogTimerTickCheckingReconnection();

                            _reconnInfo = new ReconnectionInfo(_reconnInfo.ReconnectionAttempt + 1, _stopwatch.Elapsed);
                            if (_settings.MaxReconnections >= 0 && _reconnInfo.ReconnectionAttempt > _settings.MaxReconnections)
                            {
                                await CloseConnectionAsync("Reconnection limit reached.").ConfigureAwait(false);
                            }
                            else
                            {
                                RaiseReconnecting();
                                _operations.CheckTimeoutsAndRetry(_connection);
                                DiscoverEndPoint(null);
                            }
                        }
                        if (_connectingPhase == ConnectingPhase.Authentication && _stopwatch.Elapsed - _authInfo.TimeStamp >= _settings.OperationTimeout)
                        {
                            RaiseAuthenticationFailed("Authentication timed out.");
                            GoToIdentifyState();
                        }
                        if (_connectingPhase == ConnectingPhase.Identification && _stopwatch.Elapsed - _identifyInfo.TimeStamp >= _settings.OperationTimeout)
                        {
                            const string msg = "Timed out waiting for client to be identified";
                            if (_verboseDebug) LogTimedoutWaitingForClientToBeIdentified();
                            await CloseTcpConnection(msg).ConfigureAwait(false);
                        }
                        if (_connectingPhase > ConnectingPhase.ConnectionEstablishing)
                        {
                            await ManageHeartbeatsAsync().ConfigureAwait(false);
                        }
                        break;
                    }
                case ConnectionState.Connected:
                    {
                        // operations timeouts are checked only if connection is established and check period time passed
                        if (_stopwatch.Elapsed - _lastTimeoutsTimeStamp >= _settings.OperationTimeoutCheckPeriod)
                        {
                            // On mono even impossible connection first says that it is established
                            // so clearing of reconnection count on ConnectionEstablished event causes infinite reconnections.
                            // So we reset reconnection count to zero on each timeout check period when connection is established
                            _reconnInfo = new ReconnectionInfo(0, _stopwatch.Elapsed);
                            _operations.CheckTimeoutsAndRetry(_connection);
                            _subscriptions.CheckTimeoutsAndRetry(_connection);
                            _lastTimeoutsTimeStamp = _stopwatch.Elapsed;
                        }
                        await ManageHeartbeatsAsync().ConfigureAwait(false);
                        break;
                    }
                case ConnectionState.Closed: break;
                default: ThrowException(_state); break;
            }
        }

        private async Task ManageHeartbeatsAsync()
        {
            if (_connection == null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument._connection); }

            var timeout = _heartbeatInfo.IsIntervalStage ? _settings.HeartbeatInterval : _settings.HeartbeatTimeout;
            if (_stopwatch.Elapsed - _heartbeatInfo.TimeStamp < timeout) { return; }

            var packageNumber = _packageNumber;
            if (_heartbeatInfo.LastPackageNumber != packageNumber)
            {
                _heartbeatInfo = new HeartbeatInfo(packageNumber, true, _stopwatch.Elapsed);
                return;
            }

            if (_heartbeatInfo.IsIntervalStage)
            {
                // TcpMessage.Heartbeat analog
                _connection.EnqueueSend(new TcpPackage(TcpCommand.HeartbeatRequestCommand, Guid.NewGuid(), null));
                _heartbeatInfo = new HeartbeatInfo(_heartbeatInfo.LastPackageNumber, false, _stopwatch.Elapsed);
                return;
            }
            // TcpMessage.HeartbeatTimeout analog
            await CloseTcpConnection(packageNumber).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task CloseTcpConnection(int packageNumber)
        {
            // TcpMessage.HeartbeatTimeout analog
            var msg = string.Format("EventStoreConnection '{0}': closing TCP connection [{1}, {2}, {3}] due to HEARTBEAT TIMEOUT at pkgNum {4}.",
                                    _esConnection.ConnectionName, _connection.RemoteEndPoint, _connection.LocalEndPoint,
                                    _connection.ConnectionId, packageNumber);
            if (s_logger.IsInformationLevelEnabled()) { s_logger.LogInformation(msg); }
            await CloseTcpConnection(msg).ConfigureAwait(false);
        }

        private async Task StartOperationAsync(IClientOperation operation, int maxRetries, TimeSpan timeout)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    operation.Fail(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                    if (_verboseDebug) LogStartOperationEnqueue(operation, maxRetries, timeout);
                    _operations.EnqueueOperation(new OperationItem(operation, maxRetries, timeout));
                    break;
                case ConnectionState.Connected:
                    if (_verboseDebug) LogStartOperationSchedule(operation, maxRetries, timeout);
                    _operations.ScheduleOperation(new OperationItem(operation, maxRetries, timeout), _connection);
                    break;
                case ConnectionState.Closed:
                    operation.Fail(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionMessageWrapper msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var volatileSubscriptionOperationWrapperType = typeof(SubscriptionOperationWrapper<>).GetCachedGenericType(msg.EventType);
                    var volatileSubscriptionOperationWrapper = ActivatorUtils.FastCreateInstance<IVolatileSubscriptionOperationWrapper>(volatileSubscriptionOperationWrapperType);
                    var operation = volatileSubscriptionOperationWrapper.Create(msg, _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionMessage msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var operation = msg.EventAppeared != null
                                  ? new SubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                              msg.EventAppeared, msg.SubscriptionDropped, () => _connection)
                                  : new SubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                              msg.EventAppearedAsync, msg.SubscriptionDropped, () => _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionMessage2 msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var operation = msg.EventAppeared != null
                                  ? new SubscriptionOperation2(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                               msg.EventAppeared, msg.SubscriptionDropped, () => _connection)
                                  : new SubscriptionOperation2(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                               msg.EventAppearedAsync, msg.SubscriptionDropped, () => _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartSubscriptionRawMessage msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var operation = msg.EventAppeared != null
                                  ? new VolatileSubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                      msg.EventAppeared, msg.SubscriptionDropped, () => _connection)
                                  : new VolatileSubscriptionOperation(msg.Source, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                      msg.EventAppearedAsync, msg.SubscriptionDropped, () => _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }

        private async Task StartSubscriptionAsync(StartPersistentSubscriptionMessageWrapper msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var persistentSubscriptionOperationWrapperType = typeof(PersistentSubscriptionOperationWrapper<>).GetCachedGenericType(msg.EventType);
                    var persistentSubscriptionOperationWrapper = ActivatorUtils.FastCreateInstance<IPersistentSubscriptionOperationWrapper>(persistentSubscriptionOperationWrapperType);
                    var operation = persistentSubscriptionOperationWrapper.Create(msg, _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartPersistentSubscriptionMessage msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var operation = new PersistentSubscriptionOperation(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                        msg.EventAppearedAsync, msg.SubscriptionDropped, () => _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartPersistentSubscriptionMessage2 msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var operation = new PersistentSubscriptionOperation2(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                         msg.EventAppearedAsync, msg.SubscriptionDropped, () => _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }
        private async Task StartSubscriptionAsync(StartPersistentSubscriptionRawMessage msg)
        {
            switch (_state)
            {
                case ConnectionState.Init:
                    msg.Source.SetException(ExConnectionIsNotActive());
                    break;
                case ConnectionState.Connecting:
                case ConnectionState.Connected:
                    var operation = new ConnectToPersistentSubscriptionOperation(msg.Source, msg.SubscriptionId, msg.StreamId, msg.Settings, msg.UserCredentials,
                                                                                 msg.EventAppearedAsync, msg.SubscriptionDropped, () => _connection);
                    if (_verboseDebug) LogStartSubscription(operation, msg.MaxRetries, msg.Timeout);
                    var subscription = new SubscriptionItem(operation, msg.MaxRetries, msg.Timeout);
                    if (_state == ConnectionState.Connecting)
                    {
                        _subscriptions.EnqueueSubscription(subscription);
                    }
                    else
                    {
                        _subscriptions.StartSubscription(subscription, _connection);
                    }
                    break;
                case ConnectionState.Closed:
                    msg.Source.SetException(EsConnectionDisposed());
                    break;
                default:
                    await TaskConstants.Completed;
                    ThrowException(_state); break;
            }
        }

        private async Task HandleTcpPackageAsync(TcpPackageConnection connection, TcpPackage package)
        {
            if (_connection != connection || _state == ConnectionState.Closed || _state == ConnectionState.Init)
            {
                if (_verboseDebug) LogIgnoredTcpPackage(connection.ConnectionId, package.Command, package.CorrelationId);
                return;
            }

            if (_verboseDebug) LogHandleTcpPackage(_connection.ConnectionId, package.Command, package.CorrelationId);
            _packageNumber += 1;

            if (package.Command == TcpCommand.HeartbeatResponseCommand) { return; }
            if (package.Command == TcpCommand.HeartbeatRequestCommand)
            {
                _connection.EnqueueSend(new TcpPackage(TcpCommand.HeartbeatResponseCommand, package.CorrelationId, null));
                return;
            }

            if (package.Command == TcpCommand.Authenticated || package.Command == TcpCommand.NotAuthenticated)
            {
                if (_state == ConnectionState.Connecting
                    && _connectingPhase == ConnectingPhase.Authentication
                    && _authInfo.CorrelationId == package.CorrelationId)
                {
                    if (package.Command == TcpCommand.NotAuthenticated)
                    {
                        RaiseAuthenticationFailed("Not authenticated");
                    }

                    GoToIdentifyState();
                    return;
                }
            }

            if (package.Command == TcpCommand.ClientIdentified)
            {
                if (_state == ConnectionState.Connecting && _identifyInfo.CorrelationId == package.CorrelationId)
                {
                    GoToConnectedState();
                    return;
                }
            }

            if (package.Command == TcpCommand.BadRequest && package.CorrelationId == Guid.Empty)
            {
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
                if (_state == ConnectionState.Connected)
                {
                    _operations.TryScheduleWaitingOperations(connection);
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

            if (_state != ConnectionState.Connected || _connection.RemoteEndPoint.Equals(endPoint)) { return; }

            var msg = $"EventStoreConnection '{_esConnection.ConnectionName}': going to reconnect to [{endPoint}]. Current endpoint: [{_connection.RemoteEndPoint}, L{_connection.LocalEndPoint}].";
            if (_verboseInfo) { s_logger.LogInformation(msg); }
            await CloseTcpConnection(msg).ConfigureAwait(false);

            _state = ConnectionState.Connecting;
            _connectingPhase = ConnectingPhase.EndPointDiscovery;
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
        private static void ThrowException(ConnectionState state)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception($"Unknown state: {state}.");
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

        private readonly struct HeartbeatInfo
        {
            public readonly int LastPackageNumber;
            public readonly bool IsIntervalStage;
            public readonly TimeSpan TimeStamp;

            public HeartbeatInfo(int lastPackageNumber, bool isIntervalStage, TimeSpan timeStamp)
            {
                LastPackageNumber = lastPackageNumber;
                IsIntervalStage = isIntervalStage;
                TimeStamp = timeStamp;
            }
        }

        private readonly struct ReconnectionInfo
        {
            public readonly int ReconnectionAttempt;
            public readonly TimeSpan TimeStamp;

            public ReconnectionInfo(int reconnectionAttempt, TimeSpan timeStamp)
            {
                ReconnectionAttempt = reconnectionAttempt;
                TimeStamp = timeStamp;
            }
        }

        private readonly struct AuthInfo
        {
            public readonly Guid CorrelationId;
            public readonly TimeSpan TimeStamp;

            public AuthInfo(Guid correlationId, TimeSpan timeStamp)
            {
                CorrelationId = correlationId;
                TimeStamp = timeStamp;
            }
        }

        private readonly struct IdentifyInfo
        {
            public readonly Guid CorrelationId;
            public readonly TimeSpan TimeStamp;

            public IdentifyInfo(Guid correlationId, TimeSpan timeStamp)
            {
                CorrelationId = correlationId;
                TimeStamp = timeStamp;
            }
        }

        private enum ConnectionState
        {
            Init,
            Connecting,
            Connected,
            Closed
        }

        private enum ConnectingPhase
        {
            Invalid,
            Reconnecting,
            EndPointDiscovery,
            ConnectionEstablishing,
            Authentication,
            Identification,
            Connected
        }
    }
}
