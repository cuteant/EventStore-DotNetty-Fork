using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class EventStoreNodeConnectionMultiplexer
  {
    private readonly IList<IEventStoreConnection> _innerConnections;
    private readonly int _connectionCount;

    internal EventStoreNodeConnectionMultiplexer(IList<IEventStoreConnection> connections)
    {
      _innerConnections = connections;
      _connectionCount = connections.Count;
    }

    private static int CalculateConnectionIndex(string streamId, int count)
    {
      if (string.IsNullOrEmpty(streamId)) { throw new ArgumentNullException(nameof(streamId)); }

      return Math.Abs(streamId.GetHashCode() % count);
    }
  }
}
