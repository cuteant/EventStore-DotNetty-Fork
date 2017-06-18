using System;
using System.Collections.Generic;
using System.Net;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Internal;

namespace EventStore.ClientAPI
{
  partial class EventStoreConnection
  {
    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using default <see cref="ConnectionSettings"/>.</summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="uri">The Uri to connect to. It can be tcp:// to point to a single node or discover:// to discover nodes</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(Uri uri, int numConnections)
    {
      return Create(ConnectionSettings.Default, uri, numConnections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using default <see cref="ConnectionSettings"/> 
    /// provided via a connectionstring.</summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="connectionString">The connection string to for this connection.</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(string connectionString, int numConnections)
    {
      return Create(connectionString, null, numConnections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using default <see cref="ConnectionSettings"/> 
    /// provided via a connectionstring.</summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="builder">Pre-populated settings builder, optional. If not specified, a new builder will be created.</param>
    /// <param name="connectionString">The connection string to for this connection.</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(string connectionString, ConnectionSettingsBuilder builder, int numConnections)
    {
      var settings = ConnectionString.GetConnectionSettings(connectionString, builder);
      var uri = EventStoreConnection.GetUriFromConnectionString(connectionString);
      if (uri == null && (settings.GossipSeeds == null || settings.GossipSeeds.Length == 0))
      {
        throw new Exception($"Did not find ConnectTo or GossipSeeds in the connection string.\n'{connectionString}'");
      }
      if (uri != null && settings.GossipSeeds != null && settings.GossipSeeds.Length > 0)
      {
        throw new NotSupportedException($"Setting ConnectTo as well as GossipSeeds on the connection string is currently not supported.\n{connectionString}");
      }
      return Create(settings, uri, numConnections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using <see cref="ConnectionSettings"/> passed.</summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(ConnectionSettings connectionSettings, int numConnections)
    {
      return Create(connectionSettings, (Uri)null, numConnections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using default <see cref="ConnectionSettings"/>.</summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <param name="uri">The Uri to connect to. It can be tcp:// to point to a single node or discover:// to discover nodes via dns</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(ConnectionSettings connectionSettings, Uri uri, int numConnections)
    {
      if (numConnections <= 1) { throw new ArgumentOutOfRangeException(nameof(numConnections), $"The {nameof(numConnections)} must be at least two connections."); }

      var connections = new List<IEventStoreConnection2>(numConnections);
      for (var idx = 0; idx < numConnections; idx++)
      {
        connections.Add(EventStoreConnection.Create(connectionSettings, uri, $"ES-{idx + 1}-{Guid.NewGuid()}"));
      }
      return new EventStoreNodeConnectionMultiplexer(connections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using default <see cref="ConnectionSettings"/>.</summary>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <param name="tcpEndPoint">The <see cref="IPEndPoint"/> to connect to.</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(IPEndPoint tcpEndPoint, int numConnections)
    {
      return Create(ConnectionSettings.Default, tcpEndPoint, numConnections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to single node using specific <see cref="ConnectionSettings"/>.</summary>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <param name="tcpEndPoint">The <see cref="IPEndPoint"/> to connect to.</param>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(ConnectionSettings connectionSettings, IPEndPoint tcpEndPoint, int numConnections)
    {
      Ensure.NotNull(connectionSettings, nameof(connectionSettings));
      Ensure.NotNull(tcpEndPoint, nameof(tcpEndPoint));
      if (numConnections <= 1) { throw new ArgumentOutOfRangeException(nameof(numConnections), $"The {nameof(numConnections)} must be at least two connections."); }

      var connections = new List<IEventStoreConnection2>(numConnections);
      for (var idx = 0; idx < numConnections; idx++)
      {
        connections.Add(EventStoreConnection.Create(connectionSettings, tcpEndPoint, $"ES-{idx + 1}-{Guid.NewGuid()}"));
      }
      return new EventStoreNodeConnectionMultiplexer(connections);
    }

    /// <summary>Creates a new <see cref="IEventStoreConnectionMultiplexer"/> to EventStore cluster 
    /// using specific <see cref="ConnectionSettings"/> and <see cref="ClusterSettings"/>.</summary>
    /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
    /// <param name="clusterSettings">The <see cref="ClusterSettings"/> that determine cluster behavior.</param>
    /// <param name="numConnections">The number of eventstore connection.</param>
    /// <returns>a new <see cref="IEventStoreConnectionMultiplexer"/></returns>
    public static IEventStoreConnectionMultiplexer Create(ConnectionSettings connectionSettings, ClusterSettings clusterSettings, int numConnections)
    {
      Ensure.NotNull(connectionSettings, nameof(connectionSettings));
      Ensure.NotNull(clusterSettings, nameof(clusterSettings));
      if (numConnections <= 1) { throw new ArgumentOutOfRangeException(nameof(numConnections), $"The {nameof(numConnections)} must be at least two connections."); }

      var connections = new List<IEventStoreConnection2>(numConnections);
      for (var idx = 0; idx < numConnections; idx++)
      {
        connections.Add(EventStoreConnection.Create(connectionSettings, clusterSettings, $"ES-{idx + 1}-{Guid.NewGuid()}"));
      }
      return new EventStoreNodeConnectionMultiplexer(connections);
    }
  }
}
