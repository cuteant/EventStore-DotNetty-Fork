using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventStore.ClusterNode
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IHostedService, EventStoreConsoleHostedService>(_ => new EventStoreConsoleHostedService());
                });

            await builder.RunConsoleAsync();
        }

        sealed class EventStoreConsoleHostedService : IHostedService
        {
            readonly IEventStoreService _service;

            public EventStoreConsoleHostedService()
            {
                _service = EventStoreServiceFactory.CreateScheduler();
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                _service.Start();

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                _service.Stop();

                return Task.CompletedTask;
            }
        }
    }
}