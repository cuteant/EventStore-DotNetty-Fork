using System;

namespace EventStore.ClusterNode
{
    public static class EventStoreServiceFactory
    {
        static EventStoreServiceFactory()
        {
        }

        public static IEventStoreService CreateScheduler()
        {
            return new EventStoreService();
        }
    }
}