using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Internal;
using System;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  public static class EventStoreConnectionMultiplexer
  {
    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using default <see cref="ConnectionSettings"/>
    /// </summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="uri">The Uri to connect to. It can be tcp:// to point to a single node or discover:// to discover nodes</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(Uri uri, int numConnections)
    {
      return Create(ConnectionSettings.Default, uri, numConnections);
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using default <see cref="ConnectionSettings"/> provided via a connectionstring
    /// </summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="connectionString">The connection string to for this connection.</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(string connectionString, int numConnections)
    {
      return Create(connectionString, null, numConnections);
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using default <see cref="ConnectionSettings"/> provided via a connectionstring
    /// </summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="builder">Pre-populated settings builder, optional. If not specified, a new builder will be created.</param>
    /// <param name="connectionString">The connection string to for this connection.</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(string connectionString, ConnectionSettingsBuilder builder, int numConnections)
    {
      var settings = ConnectionString.GetConnectionSettings(connectionString, builder);
      var uri = GetUriFromConnectionString(connectionString);
      if (uri == null && (settings.GossipSeeds == null || settings.GossipSeeds.Length == 0))
      {
        throw new Exception(string.Format("Did not find ConnectTo or GossipSeeds in the connection string.\n'{0}'", connectionString));
      }
      if (uri != null && settings.GossipSeeds != null && settings.GossipSeeds.Length > 0)
      {
        throw new NotSupportedException(string.Format("Setting ConnectTo as well as GossipSeeds on the connection string is currently not supported.\n{0}", connectionString));
      }
      return Create(settings, uri, connectionName);
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using <see cref="ConnectionSettings"/> passed
    /// </summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(ConnectionSettings connectionSettings, int numConnections)
    {
      return Create(connectionSettings, (Uri)null, connectionName);
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using default <see cref="ConnectionSettings"/>
    /// </summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <param name="uri">The Uri to connect to. It can be tcp:// to point to a single node or discover:// to discover nodes via dns</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(ConnectionSettings connectionSettings, Uri uri, int numConnections)
    {
      var scheme = uri == null ? "" : uri.Scheme.ToLowerInvariant();

      connectionSettings = connectionSettings ?? ConnectionSettings.Default;
      var credential = GetCredentialFromUri(uri);
      if (credential != null)
      {
        connectionSettings = new ConnectionSettings(connectionSettings.VerboseLogging, connectionSettings.MaxQueueSize, connectionSettings.MaxConcurrentItems,
        connectionSettings.MaxRetries, connectionSettings.MaxReconnections, connectionSettings.RequireMaster, connectionSettings.ReconnectionDelay, connectionSettings.OperationTimeout,
        connectionSettings.OperationTimeoutCheckPeriod, credential, connectionSettings.UseSslConnection, connectionSettings.TargetHost,
        connectionSettings.ValidateServer, connectionSettings.FailOnNoServerResponse, connectionSettings.HeartbeatInterval, connectionSettings.HeartbeatTimeout,
        connectionSettings.ClientConnectionTimeout, connectionSettings.ClusterDns, connectionSettings.GossipSeeds, connectionSettings.MaxDiscoverAttempts,
        connectionSettings.ExternalGossipPort, connectionSettings.GossipTimeout, connectionSettings.PreferRandomNode);
      }
      if (scheme == "discover")
      {
        var clusterSettings = new ClusterSettings(uri.Host, connectionSettings.MaxDiscoverAttempts, uri.Port, connectionSettings.GossipTimeout, connectionSettings.PreferRandomNode);
        Ensure.NotNull(connectionSettings, "connectionSettings");
        Ensure.NotNull(clusterSettings, "clusterSettings");

        var endPointDiscoverer = new ClusterDnsEndPointDiscoverer(clusterSettings.ClusterDns,
                                                                  clusterSettings.MaxDiscoverAttempts,
                                                                  clusterSettings.ExternalGossipPort,
                                                                  clusterSettings.GossipSeeds,
                                                                  clusterSettings.GossipTimeout,
                                                                  clusterSettings.PreferRandomNode);

        return new EventStoreNodeConnection(connectionSettings, clusterSettings, endPointDiscoverer, connectionName);
      }

      if (scheme == "tcp")
      {
#if DESKTOPCLR
        var tcpEndPoint = GetSingleNodeIPEndPointFrom(uri);
        return new EventStoreNodeConnection(connectionSettings, null, new StaticEndPointDiscoverer(tcpEndPoint, connectionSettings.UseSslConnection), connectionName);
#else
        return new EventStoreNodeConnection(connectionSettings, null, new SingleEndpointDiscoverer(uri, connectionSettings.UseSslConnection), connectionName);
#endif
      }
      if (connectionSettings.GossipSeeds != null && connectionSettings.GossipSeeds.Length > 0)
      {
        var clusterSettings = new ClusterSettings(connectionSettings.GossipSeeds,
            connectionSettings.MaxDiscoverAttempts,
            connectionSettings.GossipTimeout,
            connectionSettings.PreferRandomNode);
        Ensure.NotNull(connectionSettings, "connectionSettings");
        Ensure.NotNull(clusterSettings, "clusterSettings");

        var endPointDiscoverer = new ClusterDnsEndPointDiscoverer(
            clusterSettings.ClusterDns,
            clusterSettings.MaxDiscoverAttempts,
            clusterSettings.ExternalGossipPort,
            clusterSettings.GossipSeeds,
            clusterSettings.GossipTimeout,
            clusterSettings.PreferRandomNode);

        return new EventStoreNodeConnection(connectionSettings, clusterSettings, endPointDiscoverer,
            connectionName);
      }
      throw new Exception(string.Format("Unknown scheme for connection '{0}'", scheme));
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using default <see cref="ConnectionSettings"/>
    /// </summary>
    /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
    /// <param name="tcpEndPoint">The <see cref="IPEndPoint"/> to connect to.</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(IPEndPoint tcpEndPoint, int numConnections)
    {
      return Create(ConnectionSettings.Default, tcpEndPoint, connectionName);
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to single node using specific <see cref="ConnectionSettings"/>
    /// </summary>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <param name="tcpEndPoint">The <see cref="IPEndPoint"/> to connect to.</param>
    /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(ConnectionSettings connectionSettings, IPEndPoint tcpEndPoint, int numConnections)
    {
      Ensure.NotNull(connectionSettings, "settings");
      Ensure.NotNull(tcpEndPoint, "tcpEndPoint");
      return new EventStoreNodeConnection(connectionSettings, null, new StaticEndPointDiscoverer(tcpEndPoint, connectionSettings.UseSslConnection), connectionName);
    }

    /// <summary>
    /// Creates a new <see cref="IEventStoreConnection"/> to EventStore cluster 
    /// using specific <see cref="ConnectionSettings"/> and <see cref="ClusterSettings"/>
    /// </summary>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <param name="clusterSettings">The <see cref="ClusterSettings"/> that determine cluster behavior.</param>
    /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
    /// <returns>a new <see cref="IEventStoreConnection"/></returns>
    public static IEventStoreConnection Create(ConnectionSettings connectionSettings, ClusterSettings clusterSettings, int numConnections)
    {
      Ensure.NotNull(connectionSettings, "connectionSettings");
      Ensure.NotNull(clusterSettings, "clusterSettings");

      var endPointDiscoverer = new ClusterDnsEndPointDiscoverer(clusterSettings.ClusterDns,
                                                                clusterSettings.MaxDiscoverAttempts,
                                                                clusterSettings.ExternalGossipPort,
                                                                clusterSettings.GossipSeeds,
                                                                clusterSettings.GossipTimeout,
                                                                clusterSettings.PreferRandomNode);

      return new EventStoreNodeConnection(connectionSettings, clusterSettings, endPointDiscoverer, connectionName);
    }
  }
}
