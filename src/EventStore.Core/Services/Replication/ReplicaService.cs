using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using EventStore.Common.Utils;
using EventStore.Core.Authentication;
using EventStore.Core.Bus;
using EventStore.Core.Cluster.Settings;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Storage.EpochManager;
using EventStore.Core.Services.Transport.Tcp;
using EventStore.Core.TransactionLog.Chunks;
using EventStore.Core.TransactionLog.LogRecords;
using EventStore.Transport.Tcp;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Replication
{
    public class ReplicaService : DotNettyClientTransport,
                                    IHandle<SystemMessage.StateChangeMessage>,
                                    IHandle<ReplicationMessage.ReconnectToMaster>,
                                    IHandle<ReplicationMessage.SubscribeToMaster>,
                                    IHandle<ReplicationMessage.AckLogPosition>,
                                    IHandle<StorageMessage.PrepareAck>,
                                    IHandle<StorageMessage.CommitAck>,
                                    IHandle<ClientMessage.TcpForwardMessage>
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<ReplicaService>();

        private readonly IPublisher _publisher;
        private readonly TFChunkDb _db;
        private readonly IEpochManager _epochManager;
        private readonly IPublisher _networkSendQueue;
        private readonly IAuthenticationProvider _authProvider;

        private readonly VNodeInfo _nodeInfo;
        private readonly bool _useSsl;
        private readonly string _sslTargetHost;
        private readonly bool _sslValidateServer;
        private readonly TimeSpan _heartbeatTimeout;
        private readonly TimeSpan _heartbeatInterval;

        private readonly InternalTcpDispatcher _tcpDispatcher = new InternalTcpDispatcher();

        private VNodeState _state = VNodeState.Initializing;
        private TcpConnectionManager _connection;

        public ReplicaService(DotNettyTransportSettings settings,
                                IPublisher publisher,
                                TFChunkDb db,
                                IEpochManager epochManager,
                                IPublisher networkSendQueue,
                                IAuthenticationProvider authProvider,
                                VNodeInfo nodeInfo,
                                bool useSsl,
                                string sslTargetHost,
                                bool sslValidateServer,
                                TimeSpan heartbeatTimeout,
                                TimeSpan heartbeatInterval)
            : base(settings, useSsl, sslTargetHost, sslValidateServer)
        {
            if (null == publisher) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.publisher); }
            if (null == db) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.db); }
            if (null == epochManager) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.epochManager); }
            if (null == networkSendQueue) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.networkSendQueue); }
            if (null == authProvider) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.authProvider); }
            if (null == nodeInfo) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.nodeInfo); }

            _publisher = publisher;
            _db = db;
            _epochManager = epochManager;
            _networkSendQueue = networkSendQueue;
            _authProvider = authProvider;

            _nodeInfo = nodeInfo;
            _useSsl = useSsl;
            _sslTargetHost = sslTargetHost;
            _sslValidateServer = sslValidateServer;
            _heartbeatTimeout = heartbeatTimeout;
            _heartbeatInterval = heartbeatInterval;
        }

        public void Handle(SystemMessage.StateChangeMessage message)
        {
            _state = message.State;

            switch (message.State)
            {
                case VNodeState.Initializing:
                case VNodeState.Unknown:
                case VNodeState.PreMaster:
                case VNodeState.Master:
                case VNodeState.ShuttingDown:
                case VNodeState.Shutdown:
                    {
                        Disconnect();
                        break;
                    }
                case VNodeState.PreReplica:
                    {
                        var m = (SystemMessage.BecomePreReplica)message;
                        ConnectToMaster(m.Master);
                        break;
                    }
                case VNodeState.CatchingUp:
                case VNodeState.Clone:
                case VNodeState.Slave:
                    {
                        // nothing changed, essentially
                        break;
                    }
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(); break;
            }
        }

        private void Disconnect()
        {
            if (_connection != null)
            {
                _connection.Stop($"Node state changed to {_state}. Closing replication connection.");
                _connection = null;
            }
        }

        private void OnConnectionEstablished(TcpConnectionManager manager)
        {
            _publisher.Publish(new SystemMessage.VNodeConnectionEstablished(manager.RemoteEndPoint, manager.ConnectionId));
        }

        private void OnConnectionClosed(TcpConnectionManager manager, DisassociateInfo socketError)
        {
            _publisher.Publish(new SystemMessage.VNodeConnectionLost(manager.RemoteEndPoint, manager.ConnectionId));
        }

        public void Handle(ReplicationMessage.ReconnectToMaster message)
        {
            ConnectToMaster(message.Master);
        }

        private void ConnectToMaster(VNodeInfo master)
        {
            Debug.Assert(_state == VNodeState.PreReplica);

            var masterEndPoint = GetMasterEndPoint(master, _useSsl);

            if (_connection != null)
            {
                _connection.Stop($"Reconnecting from old master [{_connection.RemoteEndPoint}] to new master: [{masterEndPoint}].");
            }

            try
            {
                var conn = ConnectAsync(masterEndPoint).ConfigureAwait(false).GetAwaiter().GetResult();
                _connection = new TcpConnectionManager(_useSsl ? "master-secure" : "master-normal",
                                                       _tcpDispatcher,
                                                       _publisher,
                                                       conn,
                                                       _networkSendQueue,
                                                       _authProvider,
                                                       _heartbeatInterval,
                                                       _heartbeatTimeout,
                                                       OnConnectionEstablished,
                                                       OnConnectionClosed);
                _connection.OnConnectionEstablished();
            }
            catch (InvalidConnectionException exc)
            {
                OnConnectionFailed(masterEndPoint, exc);
            }
        }

        public void OnConnectionFailed(IPEndPoint remoteEndPoint, InvalidConnectionException exc)
        {
            if (Log.IsInformationLevelEnabled())
            {
                Log.LogInformation("Connection '{0}' to [{1}] failed: {2}.", _useSsl ? "master-secure" : "master-normal", remoteEndPoint, exc.ToString());
            }
            _publisher.Publish(new SystemMessage.VNodeConnectionLost(remoteEndPoint, Guid.Empty));
        }

        private static IPEndPoint GetMasterEndPoint(VNodeInfo master, bool useSsl)
        {
            if (null == master) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.master); }
            if (useSsl && master.InternalSecureTcp == null)
            {
                Log.LogError("Internal secure connections are required, but no internal secure TCP end point is specified for master [{0}]!", master);
            }

            return useSsl ? master.InternalSecureTcp ?? master.InternalTcp : master.InternalTcp;
        }

        public void Handle(ReplicationMessage.SubscribeToMaster message)
        {
            if (_state != VNodeState.PreReplica)
            {
                ThrowHelper.ThrowException_StateIsExpectedToBeVNodeStatePreReplica(_state);
            }

            var logPosition = _db.Config.WriterCheckpoint.ReadNonFlushed();
            var epochs = _epochManager.GetLastEpochs(ClusterConsts.SubscriptionLastEpochCount).ToArray();

            if (Log.IsInformationLevelEnabled())
            {
                Log.LogInformation(string.Format(
                  "Subscribing at LogPosition: {0} (0x{0:X}) to MASTER [{1}, {2:B}] as replica with SubscriptionId: {3:B}, ConnectionId: {4:B}, LocalEndPoint: [{5}], Epochs:\n{6}...\n.",
                  logPosition, _connection.RemoteEndPoint, message.MasterId, message.SubscriptionId, _connection.ConnectionId,
                  _connection.LocalEndPoint, string.Join("\n", epochs.Select(x => x.AsString()))));
            }

            var chunk = _db.Manager.GetChunkFor(logPosition);
            if (chunk == null) ThrowHelper.ThrowException_ChunkWasNullDuringSubscribing(logPosition);
            SendTcpMessage(_connection,
                           new ReplicationMessage.SubscribeReplica(
                                   logPosition, chunk.ChunkHeader.ChunkId, epochs, _nodeInfo.InternalTcp,
                                   message.MasterId, message.SubscriptionId, isPromotable: true));
        }

        public void Handle(ReplicationMessage.AckLogPosition message)
        {
            if (!_state.IsReplica()) ThrowHelper.ThrowException(ExceptionResource.StateIsNotReplica);
            if (_connection == null) ThrowHelper.ThrowException(ExceptionResource.ConnectionIsNull);
            SendTcpMessage(_connection, message);
        }

        public void Handle(StorageMessage.PrepareAck message)
        {
            if (_state == VNodeState.Slave)
            {
                Debug.Assert(_connection != null, "_connection == null");
                SendTcpMessage(_connection, message);
            }
        }

        public void Handle(StorageMessage.CommitAck message)
        {
            if (_state == VNodeState.Slave)
            {
                Debug.Assert(_connection != null, "_connection == null");
                SendTcpMessage(_connection, message);
            }
        }

        public void Handle(ClientMessage.TcpForwardMessage message)
        {
            switch (_state)
            {
                case VNodeState.PreReplica:
                    {
                        if (_connection != null)
                        {
                            SendTcpMessage(_connection, message.Message);
                        }

                        break;
                    }

                case VNodeState.CatchingUp:
                case VNodeState.Clone:
                case VNodeState.Slave:
                    {
                        Debug.Assert(_connection != null, "Connection manager is null in slave/clone/catching up state");
                        SendTcpMessage(_connection, message.Message);
                        break;
                    }

                default:
                    ThrowHelper.ThrowException_UnexpectedState(_state); break;
            }
        }

        private void SendTcpMessage(TcpConnectionManager manager, Message msg)
        {
            _networkSendQueue.Publish(new TcpMessage.TcpSend(manager, msg));
        }
    }
}
