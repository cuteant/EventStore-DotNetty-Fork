using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.AutoSubscribing;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Es.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var logFactory = new LoggerFactory();
            logFactory.AddNLog();
            TraceLogger.Initialize(logFactory);

            var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
            var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying();
            using (var conn = EventStoreConnection.Create(connStr, connSettings))
            {
                conn.ConnectAsync().GetAwaiter().GetResult();

                var autoSubscriber = new AutoSubscriber(conn, "myapp");

                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberConsumerRegistration));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberHandlerRegistration));

                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberCatchUpConsume));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberCatchUpConsumeAsync));
                autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberPersistentConsume));
                autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberPersistentConsumeAsync));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberVolatileConsume));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberVolatileConsumeAsync));

                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberConsume));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberConsumeAsync));

                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberCatchUpConsume2));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberCatchUpConsumeAsync2));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberPersistentConsume2));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberPersistentConsumeAsync2));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberVolatileConsume2));
                //autoSubscriber.RegisterConsumerTypes(typeof(AutoSubscriberVolatileConsumeAsync2));

                autoSubscriber.ConnectToSubscriptionsAsync().GetAwaiter().GetResult();

                Console.WriteLine("waiting for events. press enter to exit");
                Console.ReadKey();
                autoSubscriber.Dispose();
            }
        }
    }
}
