using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CuteAnt.AsyncEx;
using MessagePack;
using MessagePack.Resolvers;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Common.MatchHandler;

namespace EventStore.ClientAPI.Benchmarks
{
    [Config(typeof(CoreConfig))]
    public class MatchHandlerBenchmark
    {
        private MessageA _msgA;
        private MessageB _msgB;
        private Dictionary<Type, Func<Message, Task>> _handlers;

        private PartialAction<object> _partialReceive;
        private MatchBuilder _matchBuilder;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _msgA = new MessageA
            {
                StringProp = "this is a test",
                IntProp = 100,
                GuidProp = Guid.NewGuid(),
                DateProp = DateTime.UtcNow
            };
            _msgB = new MessageB
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _handlers = new Dictionary<Type, Func<Message, Task>>();
            _handlers.Add(typeof(MessageA), async msg => await ProcessMessageA((MessageA)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageB), async msg => await ProcessMessageB((MessageB)msg).ConfigureAwait(false));

            _matchBuilder = new MatchBuilder(CachedMatchCompiler<object>.Instance);
            _matchBuilder.Match(WrapAsyncHandler<MessageA>(ProcessMessageA));
            _matchBuilder.Match(WrapAsyncHandler<MessageB>(ProcessMessageB));
            _partialReceive = _matchBuilder.Build();
        }

        private Action<T> WrapAsyncHandler<T>(Func<T, Task> asyncHandler)
        {
            return m => asyncHandler(m);
        }

        [Benchmark]
        public void EventStoreHandler()
        {
            _handlers.TryGetValue(_msgA.GetType(), out Func<Message, Task> handler);
            handler(_msgA);
            _handlers.TryGetValue(_msgB.GetType(), out handler);
            handler(_msgB);
        }

        [Benchmark]
        public void AkkaMatchHandler()
        {
            _partialReceive(_msgA);
            _partialReceive(_msgB);
            //if (!_partialReceive(_msgA)) { ThrowEx(); }
            //if (!_partialReceive(_msgB)) { ThrowEx(); }
        }

        private static void ThrowEx()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("No Handler");
            }
        }

        private static readonly IFormatterResolver s_resolver = MessagePackStandardResolver.Default;
        private static Task ProcessMessageA(MessageA msg)
        {
            //Console.WriteLine("a");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageA>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        private static Task ProcessMessageB(MessageB msg)
        {
            //Console.WriteLine("b");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageB>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        [MessagePackObject]
        internal class MessageA : Message
        {
            [Key(0)]
            public virtual string StringProp { get; set; }

            [Key(1)]
            public virtual int IntProp { get; set; }

            [Key(2)]
            public virtual Guid GuidProp { get; set; }

            [Key(3)]
            public virtual DateTime DateProp { get; set; }
        }

        [MessagePackObject]
        internal class MessageB : Message
        {
            [Key(0)]
            public virtual string StringProp { get; set; }

            [Key(1)]
            public virtual int IntProp { get; set; }

            [Key(2)]
            public virtual Guid GuidProp { get; set; }

            [Key(3)]
            public virtual DateTime DateProp { get; set; }
        }
    }
}
