using PeterKottas.DotNetCore.WindowsService.Interfaces;

namespace EventStore.ClusterNode
{
    public class DotNetCoreEsService : IMicroService
    {
        private IEventStoreService _esService;
        private IMicroServiceController _controller;

        public DotNetCoreEsService()
            :this(null)
        {
        }

        public DotNetCoreEsService(IMicroServiceController controller)
        {
            _controller = controller;
            _esService = new EventStoreService();
        }

        public void Start()
        {
            var result = _esService.Start();
        }

        public void Stop()
        {
            var result = _esService.Stop();
        }
    }
}
