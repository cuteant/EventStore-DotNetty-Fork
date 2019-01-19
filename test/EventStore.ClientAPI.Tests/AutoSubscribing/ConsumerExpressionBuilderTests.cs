using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.AutoSubscribing;
using Xunit;

namespace EventStore.ClientAPI.Tests.AutoSubscribing
{
    public class ConsumerExpressionBuilderTests
    {
        class TestAutoSubscriberConsumerRegistration : IAutoSubscriberConsumerRegistration
        {
            public bool RunCompleted;
            public void RegisterConsumers(IConsumerRegistration registration)
            {
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompileConsumerRegistration()
        {
            var concreteConsumer = new TestAutoSubscriberConsumerRegistration();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberConsumerRegistration).GetMethod("RegisterConsumers", new[] { typeof(IConsumerRegistration) });
            var action = ConsumerExpressionBuilder.CompileConsumerRegistration(concreteConsumer, mi);
            action.Invoke(null);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberHandlerRegistration : IAutoSubscriberHandlerRegistration
        {
            public bool RunCompleted;
            public void RegisterHandlers(IHandlerRegistration registration)
            {
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompileHandlerRegistration()
        {
            var concreteConsumer = new TestAutoSubscriberHandlerRegistration();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberHandlerRegistration).GetMethod("RegisterHandlers", new[] { typeof(IHandlerRegistration) });
            var action = ConsumerExpressionBuilder.CompileHandlerRegistration(concreteConsumer, mi);
            action.Invoke(null);
            Assert.True(concreteConsumer.RunCompleted);
        }

        sealed class TestMsg
        {
            public string Status { get; set; }
        }

        class TestIAutoSubscriberConsume : IAutoSubscriberConsume<TestMsg>
        {
            public void Consume(TestMsg message)
            {
                message.Status = "has been consumed";
            }
        }

        [Fact]
        public void TestCompileConsumer()
        {
            var concreteConsumer = new TestIAutoSubscriberConsume();
            var msg = new TestMsg();
            Assert.Null(msg.Status);
            var mi = typeof(TestIAutoSubscriberConsume).GetMethod("Consume", new[] { typeof(TestMsg) });
            var action = ConsumerExpressionBuilder.CompileConsumer<TestMsg>(concreteConsumer, mi);
            action.Invoke(msg);
            Assert.Equal("has been consumed", msg.Status);
        }

        class TestAutoSubscriberConsumeAsync : IAutoSubscriberConsumeAsync<TestMsg>
        {
            public Task ConsumeAsync(TestMsg message)
            {
                message.Status = "has been consumed";
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberConsumeAsync();
            var msg = new TestMsg();
            Assert.Null(msg.Status);
            var mi = typeof(TestAutoSubscriberConsumeAsync).GetMethod("ConsumeAsync", new[] { typeof(TestMsg) });
            var func = ConsumerExpressionBuilder.CompileAsyncConsumer<TestMsg>(concreteConsumer, mi);
            await func.Invoke(msg);
            Assert.Equal("has been consumed", msg.Status);
        }

        class TestAutoSubscriberCatchUpConsume : IAutoSubscriberCatchUpConsume
        {
            public bool RunCompleted;
            public void Consume(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent)
            {
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompileCatchUpConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberCatchUpConsume();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberCatchUpConsume).GetMethod("Consume", new[] { typeof(EventStoreCatchUpSubscription), typeof(ResolvedEvent<object>) });
            var action = ConsumerExpressionBuilder.CompileCatchUpConsumer(concreteConsumer, mi);
            action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberCatchUpConsumeAsync : IAutoSubscriberCatchUpConsumeAsync
        {
            public bool RunCompleted;
            public Task ConsumeAsync(EventStoreCatchUpSubscription subscription, ResolvedEvent<object> resolvedEvent)
            {
                RunCompleted = true;
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncCatchUpConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberCatchUpConsumeAsync();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberCatchUpConsumeAsync).GetMethod("ConsumeAsync", new[] { typeof(EventStoreCatchUpSubscription), typeof(ResolvedEvent<object>) });
            var action = ConsumerExpressionBuilder.CompileAsyncCatchUpConsumer(concreteConsumer, mi);
            await action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberCatchUpConsume2 : IAutoSubscriberCatchUpConsume<TestMessage>
        {
            public bool RunCompleted;
            public void Consume(EventStoreCatchUpSubscription<TestMessage> subscription, ResolvedEvent<TestMessage> resolvedEvent)
            {
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompileCatchUpConsumer2()
        {
            var concreteConsumer = new TestAutoSubscriberCatchUpConsume2();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberCatchUpConsume2).GetMethod("Consume", new[] { typeof(EventStoreCatchUpSubscription<TestMessage>), typeof(ResolvedEvent<TestMessage>) });
            var action = ConsumerExpressionBuilder.CompileCatchUpConsumer(concreteConsumer, mi);
            action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberCatchUpConsumeAsync2 : IAutoSubscriberCatchUpConsumeAsync<TestMessage>
        {
            public bool RunCompleted;
            public Task ConsumeAsync(EventStoreCatchUpSubscription<TestMessage> subscription, ResolvedEvent<TestMessage> resolvedEvent)
            {
                RunCompleted = true;
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncCatchUpConsumer2()
        {
            var concreteConsumer = new TestAutoSubscriberCatchUpConsumeAsync2();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberCatchUpConsumeAsync2).GetMethod("ConsumeAsync", new[] { typeof(EventStoreCatchUpSubscription<TestMessage>), typeof(ResolvedEvent<TestMessage>) });
            var action = ConsumerExpressionBuilder.CompileAsyncCatchUpConsumer(concreteConsumer, mi);
            await action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberPersistentConsume : IAutoSubscriberPersistentConsume
        {
            public bool RunCompleted;
            public int? RetryCount;
            public void Consume(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent, int? retryCount)
            {
                RetryCount = retryCount;
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompilePersistentConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberPersistentConsume();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberPersistentConsume).GetMethod("Consume", new[] { typeof(EventStorePersistentSubscription), typeof(ResolvedEvent<object>), typeof(int?) });
            var action = ConsumerExpressionBuilder.CompilePersistentConsumer(concreteConsumer, mi);
            action.Invoke(null, default, 10);
            Assert.Equal(10, concreteConsumer.RetryCount.Value);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberPersistentConsumeAsync : IAutoSubscriberPersistentConsumeAsync
        {
            public bool RunCompleted;
            public int? RetryCount;
            public Task ConsumeAsync(EventStorePersistentSubscription subscription, ResolvedEvent<object> resolvedEvent, int? retryCount)
            {
                RetryCount = retryCount;
                RunCompleted = true;
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncPersistentConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberPersistentConsumeAsync();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberPersistentConsumeAsync).GetMethod("ConsumeAsync", new[] { typeof(EventStorePersistentSubscription), typeof(ResolvedEvent<object>), typeof(int?) });
            var action = ConsumerExpressionBuilder.CompileAsyncPersistentConsumer(concreteConsumer, mi);
            await action.Invoke(null, default, 10);
            Assert.Equal(10, concreteConsumer.RetryCount.Value);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberPersistentConsume2 : IAutoSubscriberPersistentConsume<TestMessage>
        {
            public bool RunCompleted;
            public int? RetryCount;
            public void Consume(EventStorePersistentSubscription<TestMessage> subscription, ResolvedEvent<TestMessage> resolvedEvent, int? retryCount)
            {
                RetryCount = retryCount;
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompilePersistentConsumer2()
        {
            var concreteConsumer = new TestAutoSubscriberPersistentConsume2();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberPersistentConsume2).GetMethod("Consume", new[] { typeof(EventStorePersistentSubscription<TestMessage>), typeof(ResolvedEvent<TestMessage>), typeof(int?) });
            var action = ConsumerExpressionBuilder.CompilePersistentConsumer(concreteConsumer, mi);
            action.Invoke(null, default, 10);
            Assert.Equal(10, concreteConsumer.RetryCount.Value);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberPersistentConsumeAsync2 : IAutoSubscriberPersistentConsumeAsync<TestMessage>
        {
            public bool RunCompleted;
            public int? RetryCount;
            public Task ConsumeAsync(EventStorePersistentSubscription<TestMessage> subscription, ResolvedEvent<TestMessage> resolvedEvent, int? retryCount)
            {
                RetryCount = retryCount;
                RunCompleted = true;
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncPersistentConsumer2()
        {
            var concreteConsumer = new TestAutoSubscriberPersistentConsumeAsync2();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberPersistentConsumeAsync2).GetMethod("ConsumeAsync", new[] { typeof(EventStorePersistentSubscription<TestMessage>), typeof(ResolvedEvent<TestMessage>), typeof(int?) });
            var action = ConsumerExpressionBuilder.CompileAsyncPersistentConsumer(concreteConsumer, mi);
            await action.Invoke(null, default, 10);
            Assert.Equal(10, concreteConsumer.RetryCount.Value);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberVolatileConsume : IAutoSubscriberVolatileConsume
        {
            public bool RunCompleted;
            public void Consume(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent)
            {
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompileVolatileConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberVolatileConsume();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberVolatileConsume).GetMethod("Consume", new[] { typeof(EventStoreSubscription), typeof(ResolvedEvent<object>) });
            var action = ConsumerExpressionBuilder.CompileVolatileConsumer(concreteConsumer, mi);
            action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberVolatileConsumeAsync : IAutoSubscriberVolatileConsumeAsync
        {
            public bool RunCompleted;
            public Task ConsumeAsync(EventStoreSubscription subscription, ResolvedEvent<object> resolvedEvent)
            {
                RunCompleted = true;
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncVolatileConsumer()
        {
            var concreteConsumer = new TestAutoSubscriberVolatileConsumeAsync();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberVolatileConsumeAsync).GetMethod("ConsumeAsync", new[] { typeof(EventStoreSubscription), typeof(ResolvedEvent<object>) });
            var action = ConsumerExpressionBuilder.CompileAsyncVolatileConsumer(concreteConsumer, mi);
            await action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberVolatileConsume2 : IAutoSubscriberVolatileConsume<TestMessage>
        {
            public bool RunCompleted;
            public void Consume(EventStoreSubscription subscription, ResolvedEvent<TestMessage> resolvedEvent)
            {
                RunCompleted = true;
            }
        }

        [Fact]
        public void TestCompileVolatileConsumer2()
        {
            var concreteConsumer = new TestAutoSubscriberVolatileConsume2();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberVolatileConsume2).GetMethod("Consume", new[] { typeof(EventStoreSubscription), typeof(ResolvedEvent<TestMessage>) });
            var action = ConsumerExpressionBuilder.CompileVolatileConsumer(concreteConsumer, mi);
            action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }

        class TestAutoSubscriberVolatileConsumeAsync2 : IAutoSubscriberVolatileConsumeAsync<TestMessage>
        {
            public bool RunCompleted;
            public Task ConsumeAsync(EventStoreSubscription subscription, ResolvedEvent<TestMessage> resolvedEvent)
            {
                RunCompleted = true;
                return TaskConstants.Completed;
            }
        }

        [Fact]
        public async Task TestCompileAsyncVolatileConsumer2()
        {
            var concreteConsumer = new TestAutoSubscriberVolatileConsumeAsync2();
            Assert.False(concreteConsumer.RunCompleted);
            var mi = typeof(TestAutoSubscriberVolatileConsumeAsync2).GetMethod("ConsumeAsync", new[] { typeof(EventStoreSubscription), typeof(ResolvedEvent<TestMessage>) });
            var action = ConsumerExpressionBuilder.CompileAsyncVolatileConsumer(concreteConsumer, mi);
            await action.Invoke(null, default);
            Assert.True(concreteConsumer.RunCompleted);
        }
    }
}
