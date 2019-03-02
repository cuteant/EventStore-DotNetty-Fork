using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using Es.SharedModels;
using EventStore.ClientAPI;
using EventStore.ClientAPI.AutoSubscribing;

namespace Es.Consumer
{
    public class AutoSubscriberConsumerRegistration : IAutoSubscriberConsumerRegistration
    {
        [Stream("test-animal")]
        [AutoSubscriberConsumer(Subscription = SubscriptionType.CatchUp)]
        public void RegisterConsumers(IConsumerRegistration registration)
        {
            registration.Add<Cat>(ConsumeAsync).Add<Dog>(ConsumeAsync);
        }
        private async Task ConsumeAsync(Cat cat)
        {
            await TaskConstants.Completed;
            Console.WriteLine("Received Cat: " + cat.Name + ":" + cat.Meow);
        }
        private async Task ConsumeAsync(Dog dog)
        {
            await TaskConstants.Completed;
            Console.WriteLine("Received Dog: " + dog.Name + ":" + dog.Bark);
        }
    }
    public class AutoSubscriberHandlerRegistration : IAutoSubscriberHandlerRegistration
    {
        [Stream("test-animal")]
        [AutoSubscriberConsumer(Subscription = SubscriptionType.CatchUp)]
        public void RegisterHandlers(IHandlerRegistration registration)
        {
            registration.Add<Cat>(ConsumeAsync).Add<Dog>(ConsumeAsync);
        }
        private async Task ConsumeAsync(IResolvedEvent<Cat> iEvent)
        {
            await TaskConstants.Completed;
            var cat = iEvent.Body;
            Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
        }
        private async Task ConsumeAsync(IResolvedEvent<Dog> iEvent)
        {
            await TaskConstants.Completed;
            var dog = iEvent.Body;
            Console.WriteLine("Received2: " + iEvent.OriginalStreamId + ":" + iEvent.OriginalEventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
        }
    }


    public class AutoSubscriberCatchUpConsume : IAutoSubscriberCatchUpConsume
    {
        [Stream("test-animal")]
        [CatchUpSubscriptionConfiguration(0)]
        public void Consume(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("CatchUpReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("CatchUpReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberCatchUpConsumeAsync : IAutoSubscriberCatchUpConsumeAsync
    {
        [Stream("test-animal")]
        public Task ConsumeAsync(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncCatchUpReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncCatchUpReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }
    public class AutoSubscriberPersistentConsume : IAutoSubscriberPersistentConsume
    {
        [Stream("test-animal")]
        [PersistentSubscriptionConfiguration(StartFrom = "0")]
        public void Consume(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent, int? retryCount)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("PersistentReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("PersistentReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberPersistentConsumeAsync : IAutoSubscriberPersistentConsumeAsync
    {
        [Stream("test-animal")]
        public Task ConsumeAsync(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent, int? retryCount)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncPersistentReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncPersistentReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }
    public class AutoSubscriberVolatileConsume : IAutoSubscriberVolatileConsume
    {
        [Stream("test-animal")]
        public void Consume(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("VolatileReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("VolatileReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberVolatileConsumeAsync : IAutoSubscriberVolatileConsumeAsync
    {
        [Stream("test-animal")]
        public Task ConsumeAsync(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncVolatileReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncVolatileReceived: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }


    public class AutoSubscriberConsume : IAutoSubscriberConsume<IAnimal>
    {
        [AutoSubscriberConsumer(Subscription = SubscriptionType.CatchUp)]
        public void Consume(IAnimal msg)
        {
            if (msg is Cat cat)
            {
                Console.WriteLine("Received Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("Received Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberConsumeAsync : IAutoSubscriberConsumeAsync<IAnimal>
    {
        [AutoSubscriberConsumer(Subscription = SubscriptionType.CatchUp)]
        public Task ConsumeAsync(IAnimal msg)
        {
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncReceived Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncReceived Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }


    public class AutoSubscriberCatchUpConsume2 : IAutoSubscriberCatchUpConsume<IAnimal>
    {
        public void Consume(EventStoreCatchUpSubscription<IAnimal> subscription, ResolvedEvent<IAnimal> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("CatchUpReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("CatchUpReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberCatchUpConsumeAsync2 : IAutoSubscriberCatchUpConsumeAsync<IAnimal>
    {
        public Task ConsumeAsync(EventStoreCatchUpSubscription<IAnimal> subscription, ResolvedEvent<IAnimal> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncCatchUpReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncCatchUpReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }
    public class AutoSubscriberPersistentConsume2 : IAutoSubscriberPersistentConsume<IAnimal>
    {
        public void Consume(EventStorePersistentSubscription<IAnimal> subscription, ResolvedEvent<IAnimal> resolvedEvent, int? retryCount)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("PersistentReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("PersistentReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberPersistentConsumeAsync2 : IAutoSubscriberPersistentConsumeAsync<IAnimal>
    {
        public Task ConsumeAsync(EventStorePersistentSubscription<IAnimal> subscription, ResolvedEvent<IAnimal> resolvedEvent, int? retryCount)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncPersistentReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncPersistentReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }
    public class AutoSubscriberVolatileConsume2 : IAutoSubscriberVolatileConsume<IAnimal>
    {
        public void Consume(EventStoreSubscription subscription, ResolvedEvent<IAnimal> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("VolatileReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("VolatileReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
        }
    }
    public class AutoSubscriberVolatileConsumeAsync2 : IAutoSubscriberVolatileConsumeAsync<IAnimal>
    {
        public Task ConsumeAsync(EventStoreSubscription subscription, ResolvedEvent<IAnimal> resolvedEvent)
        {
            var msg = resolvedEvent.OriginalEvent.FullEvent.Value;
            if (msg is Cat cat)
            {
                Console.WriteLine("AsyncVolatileReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Cat: " + cat.Name + ":" + cat.Meow);
            }
            if (msg is Dog dog)
            {
                Console.WriteLine("AsyncVolatileReceived1: " + resolvedEvent.Event.EventStreamId + ":" + resolvedEvent.Event.EventNumber + " Dog: " + dog.Name + ":" + dog.Bark);
            }
            return TaskConstants.Completed;
        }
    }
}
