using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.AutoSubscribing
{
    internal static class ConsumerExpressionBuilder
    {
        public static Action<IConsumerRegistration> CompileConsumerRegistration(IAutoSubscriberConsumerRegistration concreteConsumer, MethodInfo registerConsumersMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(IConsumerRegistration), "registration");
            var body = Expression.Call(instance, registerConsumersMethod, param0);
            return Expression.Lambda<Action<IConsumerRegistration>>(body, param0).Compile();
        }

        public static Action<IHandlerRegistration> CompileHandlerRegistration(IAutoSubscriberHandlerRegistration concreteConsumer, MethodInfo registerHandlersMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(IHandlerRegistration), "registration");
            var body = Expression.Call(instance, registerHandlersMethod, param0);
            return Expression.Lambda<Action<IHandlerRegistration>>(body, param0).Compile();
        }

        public static Action<T> CompileConsumer<T>(IAutoSubscriberConsume<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(T), "message");
            var body = Expression.Call(instance, consumeMethod, param0);
            return Expression.Lambda<Action<T>>(body, param0).Compile();
        }

        public static Func<T, Task> CompileAsyncConsumer<T>(IAutoSubscriberConsumeAsync<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(T), "message");
            var body = Expression.Call(instance, consumeMethod, param0);
            return Expression.Lambda<Func<T, Task>>(body, param0).Compile();
        }

        public static Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> CompileCatchUpConsumer(IAutoSubscriberCatchUpConsume concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreCatchUpSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<object>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Action<EventStoreCatchUpSubscription, ResolvedEvent<object>>>(body, param0, param1).Compile();
        }

        public static Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> CompileAsyncCatchUpConsumer(IAutoSubscriberCatchUpConsumeAsync concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreCatchUpSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<object>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task>>(body, param0, param1).Compile();
        }

        public static Action<EventStoreCatchUpSubscription<T>, ResolvedEvent<T>> CompileCatchUpConsumer<T>(IAutoSubscriberCatchUpConsume<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreCatchUpSubscription<T>), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<T>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Action<EventStoreCatchUpSubscription<T>, ResolvedEvent<T>>>(body, param0, param1).Compile();
        }

        public static Func<EventStoreCatchUpSubscription<T>, ResolvedEvent<T>, Task> CompileAsyncCatchUpConsumer<T>(IAutoSubscriberCatchUpConsumeAsync<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreCatchUpSubscription<T>), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<T>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Func<EventStoreCatchUpSubscription<T>, ResolvedEvent<T>, Task>>(body, param0, param1).Compile();
        }

        public static Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> CompilePersistentConsumer(IAutoSubscriberPersistentConsume concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStorePersistentSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<object>), "resolvedEvent");
            var param2 = Expression.Parameter(typeof(int?), "retryCount");
            var body = Expression.Call(instance, consumeMethod, param0, param1, param2);
            return Expression.Lambda<Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?>>(body, param0, param1, param2).Compile();
        }

        public static Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> CompileAsyncPersistentConsumer(IAutoSubscriberPersistentConsumeAsync concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStorePersistentSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<object>), "resolvedEvent");
            var param2 = Expression.Parameter(typeof(int?), "retryCount");
            var body = Expression.Call(instance, consumeMethod, param0, param1, param2);
            return Expression.Lambda<Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task>>(body, param0, param1, param2).Compile();
        }

        public static Action<EventStorePersistentSubscription<T>, ResolvedEvent<T>, int?> CompilePersistentConsumer<T>(IAutoSubscriberPersistentConsume<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStorePersistentSubscription<T>), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<T>), "resolvedEvent");
            var param2 = Expression.Parameter(typeof(int?), "retryCount");
            var body = Expression.Call(instance, consumeMethod, param0, param1, param2);
            return Expression.Lambda<Action<EventStorePersistentSubscription<T>, ResolvedEvent<T>, int?>>(body, param0, param1, param2).Compile();
        }

        public static Func<EventStorePersistentSubscription<T>, ResolvedEvent<T>, int?, Task> CompileAsyncPersistentConsumer<T>(IAutoSubscriberPersistentConsumeAsync<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStorePersistentSubscription<T>), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<T>), "resolvedEvent");
            var param2 = Expression.Parameter(typeof(int?), "retryCount");
            var body = Expression.Call(instance, consumeMethod, param0, param1, param2);
            return Expression.Lambda<Func<EventStorePersistentSubscription<T>, ResolvedEvent<T>, int?, Task>>(body, param0, param1, param2).Compile();
        }

        public static Action<EventStoreSubscription, ResolvedEvent<object>> CompileVolatileConsumer(IAutoSubscriberVolatileConsume concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<object>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Action<EventStoreSubscription, ResolvedEvent<object>>>(body, param0, param1).Compile();
        }

        public static Func<EventStoreSubscription, ResolvedEvent<object>, Task> CompileAsyncVolatileConsumer(IAutoSubscriberVolatileConsumeAsync concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<object>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Func<EventStoreSubscription, ResolvedEvent<object>, Task>>(body, param0, param1).Compile();
        }

        public static Action<EventStoreSubscription, ResolvedEvent<T>> CompileVolatileConsumer<T>(IAutoSubscriberVolatileConsume<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<T>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Action<EventStoreSubscription, ResolvedEvent<T>>>(body, param0, param1).Compile();
        }

        public static Func<EventStoreSubscription, ResolvedEvent<T>, Task> CompileAsyncVolatileConsumer<T>(IAutoSubscriberVolatileConsumeAsync<T> concreteConsumer, MethodInfo consumeMethod)
        {
            var instance = Expression.Constant(concreteConsumer);
            var param0 = Expression.Parameter(typeof(EventStoreSubscription), "subscription");
            var param1 = Expression.Parameter(typeof(ResolvedEvent<T>), "resolvedEvent");
            var body = Expression.Call(instance, consumeMethod, param0, param1);
            return Expression.Lambda<Func<EventStoreSubscription, ResolvedEvent<T>, Task>>(body, param0, param1).Compile();
        }
    }
}
