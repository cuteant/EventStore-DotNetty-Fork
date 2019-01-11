using System.Net;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Internal
{
    internal class StaticEndPointDiscoverer : IEndPointDiscoverer
    {
        private readonly Task<NodeEndPoints> _task;

        public StaticEndPointDiscoverer(IPEndPoint endPoint, bool isSsl)
        {
            if (null == endPoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPoint); }
            _task = Task.FromResult(new NodeEndPoints(isSsl ? null : endPoint, isSsl ? endPoint : null));
        }

        public Task<NodeEndPoints> DiscoverAsync(IPEndPoint failedTcpEndPoint) => _task;
    }
}