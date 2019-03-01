using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    /// <summary>Contains factory methods for building connections to an Event Store server.</summary>
    public static partial class EventStoreConnection
    {
        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> to single node using default <see cref="ConnectionSettings"/>.</summary>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <param name="uri">The Uri to connect to. It can be tcp:// to point to a single node or discover:// to discover nodes</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(Uri uri, string connectionName = null)
        {
            return Create(ConnectionSettings.Default, uri, connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> to single node using default <see cref="ConnectionSettings"/> 
        /// provided via a connectionstring.</summary>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <param name="connectionString">The connection string to for this connection.</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(string connectionString, string connectionName = null)
        {
            return Create(connectionString, null, connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> to single node using default <see cref="ConnectionSettings"/> 
        /// provided via a connectionstring.</summary>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <param name="builder">Pre-populated settings builder, optional. If not specified, a new builder will be created.</param>
        /// <param name="connectionString">The connection string to for this connection.</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(string connectionString, ConnectionSettingsBuilder builder, string connectionName = null)
        {
            var settings = ConnectionString.GetConnectionSettings(connectionString, builder);
            var uri = GetUriFromConnectionString(connectionString);
            if (uri == null && (settings.GossipSeeds == null || 0u >= (uint)settings.GossipSeeds.Length))
            {
                throw new Exception($"Did not find ConnectTo or GossipSeeds in the connection string.\n'{connectionString}'");
            }
            if (uri != null && settings.GossipSeeds != null && (uint)settings.GossipSeeds.Length > 0u)
            {
                throw new NotSupportedException($"Setting ConnectTo as well as GossipSeeds on the connection string is currently not supported.\n{connectionString}");
            }
            return Create(settings, uri, connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> using the gossip seeds specified in the <paramref name="connectionSettings"/>.</summary>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(ConnectionSettings connectionSettings, string connectionName = null)
        {
            if (connectionSettings.GossipSeeds == null || 0u >= (uint)connectionSettings.GossipSeeds.Length)
            {
                throw new ArgumentException("No gossip seeds specified", nameof(connectionSettings));
            }

            return Create(connectionSettings, uri: null, connectionName: connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection2"/>.</summary>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection. If null the default settings will be used and the <paramref name="uri"/> must not be null</param>
        /// <param name="uri">The Uri to connect to. It can be tcp:// to point to a single node or discover:// to discover nodes via dns or null to connect using the gossip seeds from the <paramref name="connectionSettings"/></param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        /// <remarks>You must pass a uri or set gossip seeds in the connection settings.</remarks>
        public static IEventStoreConnection2 Create(ConnectionSettings connectionSettings, Uri uri, string connectionName = null)
        {
            connectionSettings = connectionSettings ?? ConnectionSettings.Default;
            if (uri != null)
            {
                var scheme = uri.Scheme.ToLowerInvariant();
                var credential = GetCredentialFromUri(uri);
                if (credential != null)
                {
                    connectionSettings = new ConnectionSettings(connectionSettings.VerboseLogging,
                        connectionSettings.MaxQueueSize, connectionSettings.MaxConcurrentItems,
                        connectionSettings.MaxRetries, connectionSettings.MaxReconnections,
                        connectionSettings.RequireMaster, connectionSettings.ReconnectionDelay,
                        connectionSettings.QueueTimeout, connectionSettings.OperationTimeout,
                        connectionSettings.OperationTimeoutCheckPeriod, credential, connectionSettings.UseSslConnection,
                        connectionSettings.TargetHost,
                        connectionSettings.ValidateServer, connectionSettings.FailOnNoServerResponse,
                        connectionSettings.HeartbeatInterval, connectionSettings.HeartbeatTimeout,
                        connectionSettings.ClientConnectionTimeout, connectionSettings.ClusterDns,
                        connectionSettings.GossipSeeds, connectionSettings.MaxDiscoverAttempts,
                        connectionSettings.ExternalGossipPort, connectionSettings.GossipTimeout,
                        connectionSettings.NodePreference, connectionSettings.ThrowOnNoMatchingHandler,
                        connectionSettings.EnableLibuv, connectionSettings.EnableBufferPooling,
                        connectionSettings.WriteBufferHighWaterMark, connectionSettings.WriteBufferLowWaterMark,
                        connectionSettings.SendBufferSize, connectionSettings.ReceiveBufferSize,
                        connectionSettings.SocketWorkerPoolSizeMin, connectionSettings.SocketWorkerPoolSizeFactor,
                        connectionSettings.SocketWorkerPoolSizeMax);
                }
                if (scheme == "discover")
                {
                    var clusterSettings = new ClusterSettings(uri.Host, connectionSettings.MaxDiscoverAttempts, uri.Port,
                        connectionSettings.GossipTimeout, connectionSettings.NodePreference);
                    if (null == connectionSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connectionSettings); }
                    if (null == clusterSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.clusterSettings); }

                    var endPointDiscoverer = new ClusterDnsEndPointDiscoverer(clusterSettings.ClusterDns,
                        clusterSettings.MaxDiscoverAttempts,
                        clusterSettings.ExternalGossipPort,
                        clusterSettings.GossipSeeds,
                        clusterSettings.GossipTimeout,
                        clusterSettings.NodePreference);

                    return new EventStoreNodeConnection(connectionSettings, clusterSettings, endPointDiscoverer, connectionName);
                }

                if (scheme == "tcp")
                {
                    var tcpEndPoint = GetSingleNodeIPEndPointFrom(uri);
                    return new EventStoreNodeConnection(connectionSettings, null, new StaticEndPointDiscoverer(tcpEndPoint, connectionSettings.UseSslConnection), connectionName);
                }
                throw new Exception($"Unknown scheme for connection '{scheme}'");
            }
            if (connectionSettings.GossipSeeds != null && (uint)connectionSettings.GossipSeeds.Length > 0u)
            {
                var clusterSettings = new ClusterSettings(connectionSettings.GossipSeeds,
                    connectionSettings.MaxDiscoverAttempts,
                    connectionSettings.GossipTimeout,
                    connectionSettings.NodePreference);
                if (null == connectionSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connectionSettings); }
                if (null == clusterSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.clusterSettings); }

                var endPointDiscoverer = new ClusterDnsEndPointDiscoverer(
                    clusterSettings.ClusterDns,
                    clusterSettings.MaxDiscoverAttempts,
                    clusterSettings.ExternalGossipPort,
                    clusterSettings.GossipSeeds,
                    clusterSettings.GossipTimeout,
                    clusterSettings.NodePreference);

                return new EventStoreNodeConnection(connectionSettings, clusterSettings, endPointDiscoverer, connectionName);
            }
            throw new Exception("Must specify uri or gossip seeds");
        }

        private static IPEndPoint GetSingleNodeIPEndPointFrom(Uri uri)
        {
            // TODO GFY move this all the way back into the connection so it can be done on connect not on create
            var ipaddress = IPAddress.Any;
            if (!IPAddress.TryParse(uri.Host, out ipaddress))
            {
                var entries = Dns.GetHostAddresses(uri.Host);
                if (0u >= (uint)entries.Length) { throw new Exception($"Unable to parse IP address or lookup DNS host for '{uri.Host}'"); }
                //pick an IPv4 address, if one exists
                ipaddress = entries.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                if (ipaddress == null) { throw new Exception($"Could not get an IPv4 address for host '{uri.Host}'"); }
            }
            var port = uri.IsDefaultPort ? 2113 : uri.Port;
            return new IPEndPoint(ipaddress, port);
        }

        private static UserCredentials GetCredentialFromUri(Uri uri)
        {
            if (uri == null || string.IsNullOrEmpty(uri.UserInfo)) { return null; }
            var pieces = uri.UserInfo.Split(':');
            if (pieces.Length != 2) { throw new Exception($"Unable to parse user information '{uri.UserInfo}'"); }
            return new UserCredentials(pieces[0], pieces[1]);
        }

        internal static Uri GetUriFromConnectionString(string connectionString)
        {
            var connto = ConnectionString.GetConnectionStringInfo(connectionString)
                                         .FirstOrDefault(x => string.Equals(x.Key, "CONNECTTO", StringComparison.OrdinalIgnoreCase)).Value;
            return connto == null ? null : new Uri(connto);
        }
        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> to single node using default <see cref="ConnectionSettings"/>.</summary>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <param name="tcpEndPoint">The <see cref="IPEndPoint"/> to connect to.</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(IPEndPoint tcpEndPoint, string connectionName = null)
        {
            return Create(ConnectionSettings.Default, tcpEndPoint, connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> to single node using specific <see cref="ConnectionSettings"/>.</summary>
        /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <param name="tcpEndPoint">The <see cref="IPEndPoint"/> to connect to.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(ConnectionSettings connectionSettings, IPEndPoint tcpEndPoint, string connectionName = null)
        {
            if (null == connectionSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connectionSettings); }
            if (null == tcpEndPoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.tcpEndPoint); }
            return new EventStoreNodeConnection(connectionSettings, null, new StaticEndPointDiscoverer(tcpEndPoint, connectionSettings.UseSslConnection), connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection2"/> to EventStore cluster 
        /// using specific <see cref="ConnectionSettings"/> and <see cref="ClusterSettings"/>.</summary>
        /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <param name="clusterSettings">The <see cref="ClusterSettings"/> that determine cluster behavior.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreConnection2"/></returns>
        public static IEventStoreConnection2 Create(ConnectionSettings connectionSettings, ClusterSettings clusterSettings, string connectionName = null)
        {
            if (null == connectionSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connectionSettings); }
            if (null == clusterSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.clusterSettings); }

            var endPointDiscoverer = new ClusterDnsEndPointDiscoverer(clusterSettings.ClusterDns,
                                                                      clusterSettings.MaxDiscoverAttempts,
                                                                      clusterSettings.ExternalGossipPort,
                                                                      clusterSettings.GossipSeeds,
                                                                      clusterSettings.GossipTimeout,
                                                                      clusterSettings.NodePreference);

            return new EventStoreNodeConnection(connectionSettings, clusterSettings, endPointDiscoverer, connectionName);
        }

        /// <summary>Creates a new <see cref="IEventStoreConnection"/> using specific <see cref="ConnectionSettings"/>
        /// and a custom-defined <see cref="IEndPointDiscoverer"/>.</summary>
        /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <param name="endPointDiscoverer">The custom-defined <see cref="IEndPointDiscoverer"/> to use for node discovery</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreConnection"/></returns>
        public static IEventStoreConnection2 Create(ConnectionSettings connectionSettings, IEndPointDiscoverer endPointDiscoverer, string connectionName = null)
        {
            if (null == connectionSettings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connectionSettings); }
            if (null == endPointDiscoverer) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPointDiscoverer); }

            return new EventStoreNodeConnection(connectionSettings, null, endPointDiscoverer, connectionName);
        }
    }
}
