using System;
using System.Configuration;
using Topshelf;
using Serilog;

namespace EventStore.ClusterNode
{
    class Program
    {
        public static void Main(string[] args)
        {
            HostFactory.Run(hostConfiguration =>
            {
                hostConfiguration.Service<IEventStoreService>(serviceConfiguration =>
                {
                    serviceConfiguration.ConstructUsing(_ => EventStoreServiceFactory.CreateScheduler());

                    serviceConfiguration.WhenStarted((service, _) => service.Start());
                    serviceConfiguration.WhenStopped((service, _) => service.Stop());
                });

                hostConfiguration.UseSerilog(new LoggerConfiguration().ReadFrom.AppSettings());

                var dependsOnServices = ConfigurationManager.AppSettings.Get("dependsOnServices");
                if (!string.IsNullOrWhiteSpace(dependsOnServices))
                {
                    var otherServices = dependsOnServices.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (otherServices != null && otherServices.Length > 0)
                    {
                        foreach (var item in otherServices)
                        {
                            hostConfiguration.DependsOn(item);
                        }
                    }
                }

                hostConfiguration.EnableServiceRecovery(serviceRecoveryConfiguration =>
                {
                    // On the first service failure, reset service after a minute
                    serviceRecoveryConfiguration.RestartService(delayInMinutes: 1);
                    // Reset failure count after every failure
                    serviceRecoveryConfiguration.SetResetPeriod(days: 0);
                })
                .RunAsLocalSystem();

                var serviceDescription = ConfigurationManager.AppSettings.Get("serviceDescription");
                if (string.IsNullOrWhiteSpace(serviceDescription)) { serviceDescription = "ES.ClusterNode"; }
                var serviceDisplayName = ConfigurationManager.AppSettings.Get("serviceDisplayName");
                if (string.IsNullOrWhiteSpace(serviceDisplayName)) { serviceDisplayName = "ES.ClusterNode"; }
                var serviceName = ConfigurationManager.AppSettings.Get("serviceName");
                if (string.IsNullOrWhiteSpace(serviceName)) { serviceName = "ESCluster"; }
                hostConfiguration.SetDescription(serviceDescription);
                hostConfiguration.SetDisplayName(serviceDisplayName);
                hostConfiguration.SetServiceName(serviceName);
            });
        }
    }
}
