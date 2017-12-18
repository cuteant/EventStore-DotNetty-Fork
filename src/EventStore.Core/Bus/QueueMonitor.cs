using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Services.Monitoring.Stats;

namespace EventStore.Core.Bus
{
    public class QueueMonitor
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<QueueMonitor>();
        public static readonly QueueMonitor Default = new QueueMonitor();

        private readonly ConcurrentDictionary<IMonitoredQueue, IMonitoredQueue> _queues = new ConcurrentDictionary<IMonitoredQueue, IMonitoredQueue>();

        private QueueMonitor()
        {
        }

        public void Register(IMonitoredQueue monitoredQueue)
        {
            _queues[monitoredQueue] = monitoredQueue;
        }

        public void Unregister(IMonitoredQueue monitoredQueue)
        {
            _queues.TryRemove(monitoredQueue, out IMonitoredQueue v);
        }

        public QueueStats[] GetStats()
        {
            var stats = _queues.Keys.OrderBy(x => x.Name).Select(queue => queue.GetStatistics()).ToArray();
            if (Log.IsTraceLevelEnabled() && Application.IsDefined(Application.DumpStatistics))
            {
                Log.LogTrace(Environment.NewLine + string.Join(Environment.NewLine, stats.Select(x => x.ToString())));
            }
            return stats;
        }
    }
}