using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Monitoring.Stats;
using EventStore.Core.Services.Monitoring.Utils;
using EventStore.Core.Services.UserManagement;
using EventStore.Core.TransactionLog.Checkpoint;
using EventStore.Transport.Tcp;

namespace EventStore.Core.Services.Monitoring
{
    [Flags]
    public enum StatsStorage
    {
        None = 0x0,       // only for tests
        Stream = 0x1,
        File = 0x2,
        StreamAndFile = Stream | File
    }

    public class MonitoringService : IHandle<SystemMessage.SystemInit>,
                                        IHandle<SystemMessage.StateChangeMessage>,
                                        IHandle<SystemMessage.BecomeShuttingDown>,
                                        IHandle<SystemMessage.BecomeShutdown>,
                                        IHandle<ClientMessage.WriteEventsCompleted>,
                                        IHandle<MonitoringMessage.GetFreshStats>,
                                        IHandle<MonitoringMessage.GetFreshTcpConnectionStats>
    {
        private static readonly ILogger RegularLog = TraceLogger.GetLogger("REGULAR-STATS-LOGGER");
        private static readonly ILogger Log = TraceLogger.GetLogger<MonitoringService>();

        private static readonly string StreamMetadata = $"{{\"$maxAge\":{(int)TimeSpan.FromDays(10).TotalSeconds}}}";
        public static readonly TimeSpan MemoizePeriod = TimeSpan.FromSeconds(1);
        private static readonly IEnvelope NoopEnvelope = new NoopEnvelope();

        private readonly IQueuedHandler _monitoringQueue;
        private readonly IPublisher _statsCollectionBus;
        private readonly IPublisher _mainBus;
        private readonly ICheckpoint _writerCheckpoint;
        private readonly string _dbPath;
        private readonly StatsStorage _statsStorage;
        private readonly long _statsCollectionPeriodMs;
        private SystemStatsHelper _systemStats;

        private string _lastWrittenCsvHeader;
        private DateTime _lastCsvTimestamp = DateTime.UtcNow;
        private DateTime _lastStatsRequestTime = DateTime.UtcNow;
        private StatsContainer _memoizedStats;
        private readonly Timer _timer;
        private readonly string _nodeStatsStream;
        private bool _statsStreamCreated;
        private Guid _streamMetadataWriteCorrId;
        private IMonitoredTcpConnection[] _memoizedTcpConnections;
        private DateTime _lastTcpConnectionsRequestTime;
        private IPEndPoint _tcpEndpoint;
        private IPEndPoint _tcpSecureEndpoint;

        public MonitoringService(IQueuedHandler monitoringQueue,
                                   IPublisher statsCollectionBus,
                                   IPublisher mainBus,
                                   ICheckpoint writerCheckpoint,
                                   string dbPath,
                                   TimeSpan statsCollectionPeriod,
                                   IPEndPoint nodeEndpoint,
                                   StatsStorage statsStorage,
                                   IPEndPoint tcpEndpoint,
                                   IPEndPoint tcpSecureEndpoint)
        {
            if (null == monitoringQueue) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.monitoringQueue); }
            if (null == statsCollectionBus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.statsCollectionBus); }
            if (null == mainBus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.mainBus); }
            if (null == writerCheckpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writerCheckpoint); }
            if (string.IsNullOrEmpty(dbPath)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dbPath); }
            if (null == nodeEndpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.nodeEndpoint); }
            if (null == tcpEndpoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.tcpEndpoint); }

            _monitoringQueue = monitoringQueue;
            _statsCollectionBus = statsCollectionBus;
            _mainBus = mainBus;
            _writerCheckpoint = writerCheckpoint;
            _dbPath = dbPath;
            _statsStorage = statsStorage;
            _statsCollectionPeriodMs = statsCollectionPeriod > TimeSpan.Zero ? (long)statsCollectionPeriod.TotalMilliseconds : Timeout.Infinite;
            _nodeStatsStream = $"{SystemStreams.StatsStreamPrefix}-{nodeEndpoint}";
            _tcpEndpoint = tcpEndpoint;
            _tcpSecureEndpoint = tcpSecureEndpoint;
            _timer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Handle(SystemMessage.SystemInit message)
        {
            _systemStats = new SystemStatsHelper(Log, _writerCheckpoint, _dbPath);
            _timer.Change(_statsCollectionPeriodMs, Timeout.Infinite);
        }

        public void OnTimerTick(object state)
        {
            CollectRegularStats();
            _timer.Change(_statsCollectionPeriodMs, Timeout.Infinite);
        }

        private void CollectRegularStats()
        {
            try
            {
                var stats = CollectStats();
                if (stats != null)
                {
                    var rawStats = stats.GetStats(useGrouping: false, useMetadata: false);

                    if ((_statsStorage & StatsStorage.File) != 0)
                    {
                        SaveStatsToFile(rawStats);
                    }

                    if ((_statsStorage & StatsStorage.Stream) != 0)
                    {
                        if (_statsStreamCreated) { SaveStatsToStream(rawStats); }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnRegularStatsCollection(ex);
            }
        }

        private StatsContainer CollectStats()
        {
            var statsContainer = new StatsContainer();
            try
            {
                statsContainer.Add(_systemStats.GetSystemStats());
                _statsCollectionBus.Publish(new MonitoringMessage.InternalStatsRequest(new StatsCollectorEnvelope(statsContainer)));
            }
            catch (Exception ex)
            {
                Log.ErrorWhileCollectingStats(ex);
                statsContainer = null;
            }

            return statsContainer;
        }

        private void SaveStatsToFile(Dictionary<string, object> rawStats)
        {
            var infoEnabled = RegularLog.IsInformationLevelEnabled();
            if (!infoEnabled) { return; }

            var writeHeader = false;

            var header = StatsCsvEncoder.GetHeader(rawStats);
            if (header != _lastWrittenCsvHeader)
            {
                _lastWrittenCsvHeader = header;
                writeHeader = true;
            }

            var line = StatsCsvEncoder.GetLine(rawStats);
            var timestamp = GetTimestamp(line);
            if (timestamp.HasValue)
            {
                if (timestamp.Value.Day != _lastCsvTimestamp.Day)
                {
                    writeHeader = true;
                }
                _lastCsvTimestamp = timestamp.Value;
            }

            if (writeHeader)
            {
                RegularLog.LogInformation(Environment.NewLine);
                RegularLog.LogInformation(header);
            }
            if (infoEnabled) { RegularLog.LogInformation(line); }
        }

        private DateTime? GetTimestamp(string line)
        {
            var separatorIdx = line.IndexOf(',');
            if (separatorIdx == -1) { return null; }

            try
            {
                return DateTime.Parse(line.Substring(0, separatorIdx)).ToUniversalTime();
            }
            catch
            {
                return null;
            }
        }

        private void SaveStatsToStream(Dictionary<string, object> rawStats)
        {
            var data = rawStats.ToJsonBytes();
            var evnt = new Event(Guid.NewGuid(), SystemEventTypes.StatsCollection, true, data, null);
            var corrId = Guid.NewGuid();
            var msg = new ClientMessage.WriteEvents(corrId, corrId, NoopEnvelope, false, _nodeStatsStream,
                                                    ExpectedVersion.Any, new[] { evnt }, SystemAccount.Principal);
            _mainBus.Publish(msg);
        }

        public void Handle(SystemMessage.StateChangeMessage message)
        {
            if (0u >= (uint)(_statsStorage & StatsStorage.Stream)) { return; }

            if (_statsStreamCreated) { return; }

            switch (message.State)
            {
                case VNodeState.CatchingUp:
                case VNodeState.Clone:
                case VNodeState.Slave:
                case VNodeState.Master:
                    {
                        SetStatsStreamMetadata();
                        break;
                    }
            }
        }

        public void Handle(SystemMessage.BecomeShuttingDown message)
        {
            try
            {
                _timer.Dispose();
                _systemStats.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // ok, no problem if already disposed
            }

        }

        public void Handle(SystemMessage.BecomeShutdown message)
        {
            _monitoringQueue.RequestStop();
        }

        private void SetStatsStreamMetadata()
        {
            var metadata = Helper.UTF8NoBom.GetBytes(StreamMetadata);
            _streamMetadataWriteCorrId = Guid.NewGuid();
            _mainBus.Publish(
                new ClientMessage.WriteEvents(
                    _streamMetadataWriteCorrId, _streamMetadataWriteCorrId, new PublishEnvelope(_monitoringQueue),
                    false, SystemStreams.MetastreamOf(_nodeStatsStream), ExpectedVersion.NoStream,
                    new[] { new Event(Guid.NewGuid(), SystemEventTypes.StreamMetadata, true, metadata, null) },
                    SystemAccount.Principal));
        }

        public void Handle(ClientMessage.WriteEventsCompleted message)
        {
            if (message.CorrelationId != _streamMetadataWriteCorrId) { return; }

            switch (message.Result)
            {
                case OperationResult.Success:
                case OperationResult.WrongExpectedVersion: // already created
                    {
                        if (Log.IsTraceLevelEnabled()) Log.CreatedStatsStream(_nodeStatsStream, message.Result);
                        _statsStreamCreated = true;
                        break;
                    }
                case OperationResult.PrepareTimeout:
                case OperationResult.CommitTimeout:
                case OperationResult.ForwardTimeout:
                    {
                        if (Log.IsDebugLevelEnabled()) Log.Failed_to_create_stats_stream(_nodeStatsStream, message);
                        SetStatsStreamMetadata();
                        break;
                    }
                case OperationResult.AccessDenied:
                    {
                        // can't do anything about that right now
                        break;
                    }
                case OperationResult.StreamDeleted:
                case OperationResult.InvalidTransaction: // should not happen at all
                    {
                        Log.MonitoringServiceGotUnexpectedResponseCodeWhenTryingToCreateStatsStream(message.Result);
                        break;
                    }
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(); break;
            }
        }

        public void Handle(MonitoringMessage.GetFreshStats message)
        {
            try
            {
                if (!TryGetMemoizedStats(out StatsContainer stats))
                {
                    stats = CollectStats();
                    if (stats != null)
                    {
                        _memoizedStats = stats;
                        _lastStatsRequestTime = DateTime.UtcNow;
                    }
                }

                Dictionary<string, object> selectedStats = null;
                if (stats != null)
                {
                    selectedStats = stats.GetStats(message.UseGrouping, message.UseMetadata);
                    if (message.UseGrouping) { selectedStats = message.StatsSelector(selectedStats); }
                }

                message.Envelope.ReplyWith(
                    new MonitoringMessage.GetFreshStatsCompleted(success: selectedStats != null, stats: selectedStats));
            }
            catch (Exception ex)
            {
                Log.ErrorOnGettingFreshStats(ex);
            }
        }

        public void Handle(MonitoringMessage.GetFreshTcpConnectionStats message)
        {
            try
            {
                if (!TryGetMemoizedTcpConnections(out IMonitoredTcpConnection[] connections))
                {
                    connections = TcpConnectionMonitor.Default.GetTcpConnectionStats();
                    if (connections != null)
                    {
                        _memoizedTcpConnections = connections;
                        _lastTcpConnectionsRequestTime = DateTime.UtcNow;
                    }
                }
                List<MonitoringMessage.TcpConnectionStats> connStats = new List<MonitoringMessage.TcpConnectionStats>();
                foreach (var conn in connections)
                {
                    bool isExternalConnection;
                    var isSsl = conn.IsSsl;
                    if (!isSsl)
                    {
                        isExternalConnection = _tcpEndpoint.Port == conn.LocalEndPoint.Port;
                    }
                    else
                    {
                        isExternalConnection = _tcpSecureEndpoint != null && _tcpSecureEndpoint.Port == conn.LocalEndPoint.Port;
                    }
                    connStats.Add(new MonitoringMessage.TcpConnectionStats
                    {
                        IsExternalConnection = isExternalConnection,
                        RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                        LocalEndPoint = conn.LocalEndPoint.ToString(),
                        ConnectionId = conn.ConnectionId,
                        ClientConnectionName = conn.ClientConnectionName,
                        TotalBytesSent = conn.TotalBytesSent,
                        TotalBytesReceived = conn.TotalBytesReceived,
                        PendingSendBytes = conn.PendingSendBytes,
                        PendingReceivedBytes = conn.PendingReceivedBytes,
                        IsSslConnection = isSsl
                    });
                }
                message.Envelope.ReplyWith(
                    new MonitoringMessage.GetFreshTcpConnectionStatsCompleted(connStats)
                );
            }
            catch (Exception ex)
            {
                Log.ErrorOnGettingFreshTcpConnectionStats(ex);
            }
        }

        private bool TryGetMemoizedStats(out StatsContainer stats)
        {
            if (_memoizedStats == null || DateTime.UtcNow - _lastStatsRequestTime > MemoizePeriod)
            {
                stats = null;
                return false;
            }
            stats = _memoizedStats;
            return true;
        }

        private bool TryGetMemoizedTcpConnections(out IMonitoredTcpConnection[] connections)
        {
            if (_memoizedTcpConnections == null || DateTime.UtcNow - _lastTcpConnectionsRequestTime > MemoizePeriod)
            {
                connections = null;
                return false;
            }
            connections = _memoizedTcpConnections;
            return true;
        }
    }
}
