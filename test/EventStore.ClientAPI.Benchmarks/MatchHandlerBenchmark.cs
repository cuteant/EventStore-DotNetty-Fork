using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CuteAnt.AsyncEx;
using MessagePack;
using MessagePack.Resolvers;
using EventStore.Transport.Tcp.Messages;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Transport.Tcp;
using EventStore.ClientAPI.Common.MatchHandler;

namespace EventStore.ClientAPI.Benchmarks
{
    [Config(typeof(CoreConfig))]
    public class MatchHandlerBenchmark
    {
        private MessageA _msgA;
        private MessageB _msgB;
        private MessageC _msgC;
        private MessageD _msgD;
        private MessageE _msgE;
        private MessageF _msgF;
        private MessageG _msgG;
        private MessageH _msgH;
        private MessageI _msgI;
        private MessageJ _msgJ;
        private MessageK _msgK;
        private MessageL _msgL;
        private MessageM _msgM;
        private MessageN _msgN;
        private MessageO _msgO;
        private MessageP _msgP;
        private MessageQ _msgQ;
        private MessageR _msgR;
        private MessageS _msgS;
        private MessageT _msgT;
        private Dictionary<Type, Func<Message, Task>> _handlers;

        private TcpPackage _packageA;
        private TcpPackage _packageB;
        private TcpPackage _packageC;
        private TcpPackage _packageD;
        private TcpPackage _packageE;
        private Dictionary<TcpCommand, Action<TcpPackage, int>> _handler2;
        private Action<TcpPackage, int>[] _handler3;

        private Func<Message, Task> _binderMatcher;

        private Action<TcpPackage, int> _packageMatcher;

        private PartialAction<object> _partialReceive;
        private MatchBuilder _matchBuilder;


        [GlobalSetup]
        public void GlobalSetup()
        {
            try
            {
                MessagePackStandardResolver.Register(TcpPackageFormatter.Instance);
            }
            catch { }

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
            _msgC = new MessageC
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgD = new MessageD
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgE = new MessageE
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgF= new MessageF
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgG = new MessageG
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgH = new MessageH
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgI = new MessageI
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgJ = new MessageJ
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgK = new MessageK
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgL = new MessageL
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgM = new MessageM
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgN = new MessageN
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgO = new MessageO
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgP = new MessageP
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgQ = new MessageQ
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgR = new MessageR
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgS = new MessageS
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _msgT = new MessageT
            {
                StringProp = _msgA.StringProp,
                IntProp = _msgA.IntProp,
                GuidProp = _msgA.GuidProp,
                DateProp = _msgA.DateProp
            };
            _handlers = new Dictionary<Type, Func<Message, Task>>();
            _handlers.Add(typeof(MessageA), async msg => await ProcessMessageA((MessageA)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageB), async msg => await ProcessMessageB((MessageB)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageC), async msg => await ProcessMessageC((MessageC)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageD), async msg => await ProcessMessageD((MessageD)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageE), async msg => await ProcessMessageE((MessageE)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageF), async msg => await ProcessMessageAny((MessageF)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageG), async msg => await ProcessMessageAny((MessageG)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageH), async msg => await ProcessMessageAny((MessageH)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageI), async msg => await ProcessMessageAny((MessageI)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageJ), async msg => await ProcessMessageAny((MessageJ)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageK), async msg => await ProcessMessageAny((MessageK)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageL), async msg => await ProcessMessageAny((MessageL)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageM), async msg => await ProcessMessageAny((MessageM)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageN), async msg => await ProcessMessageAny((MessageN)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageO), async msg => await ProcessMessageAny((MessageO)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageP), async msg => await ProcessMessageAny((MessageP)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageQ), async msg => await ProcessMessageAny((MessageQ)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageR), async msg => await ProcessMessageAny((MessageR)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageS), async msg => await ProcessMessageAny((MessageS)msg).ConfigureAwait(false));
            _handlers.Add(typeof(MessageT), async msg => await ProcessMessageAny((MessageT)msg).ConfigureAwait(false));

            var matchBuilder = new SimpleMatchBuilder<Message, Task>();
            matchBuilder.Match<MessageA>(ProcessMessageA);
            matchBuilder.Match<MessageB>(ProcessMessageB);
            matchBuilder.Match<MessageC>(ProcessMessageC);
            matchBuilder.Match<MessageD>(ProcessMessageD);
            matchBuilder.Match<MessageE>(ProcessMessageE);
            matchBuilder.Match<MessageF>(ProcessMessageAny);
            matchBuilder.Match<MessageG>(ProcessMessageAny);
            matchBuilder.Match<MessageH>(ProcessMessageAny);
            matchBuilder.Match<MessageI>(ProcessMessageAny);
            matchBuilder.Match<MessageJ>(ProcessMessageAny);
            matchBuilder.Match<MessageK>(ProcessMessageAny);
            matchBuilder.Match<MessageL>(ProcessMessageAny);
            matchBuilder.Match<MessageM>(ProcessMessageAny);
            matchBuilder.Match<MessageN>(ProcessMessageAny);
            matchBuilder.Match<MessageO>(ProcessMessageAny);
            matchBuilder.Match<MessageP>(ProcessMessageAny);
            matchBuilder.Match<MessageQ>(ProcessMessageAny);
            matchBuilder.Match<MessageR>(ProcessMessageAny);
            matchBuilder.Match<MessageS>(ProcessMessageAny);
            matchBuilder.Match<MessageT>(ProcessMessageAny);
            _binderMatcher = matchBuilder.Build();


            _packageA = new TcpPackage(TcpCommand.Authenticate, Guid.NewGuid(), new byte[] { 1, 2, 3, 4 });
            _packageB = new TcpPackage(TcpCommand.Authenticated, Guid.NewGuid(), new byte[] { 1, 2, 3, 4 });
            _packageC = new TcpPackage(TcpCommand.BadRequest, Guid.NewGuid(), new byte[] { 1, 2, 3, 4 });
            _packageD = new TcpPackage(TcpCommand.ClientIdentified, Guid.NewGuid(), new byte[] { 1, 2, 3, 4 });
            _packageE = new TcpPackage(TcpCommand.CloneAssignment, Guid.NewGuid(), new byte[] { 1, 2, 3, 4 });
            _handler2 = new Dictionary<TcpCommand, Action<TcpPackage, int>>();
            _handler2.Add(TcpCommand.Authenticate, ProcessPackage);
            _handler2.Add(TcpCommand.Authenticated, ProcessPackage);
            _handler2.Add(TcpCommand.BadRequest, ProcessPackage);
            _handler2.Add(TcpCommand.ClientIdentified, ProcessPackage);
            _handler2.Add(TcpCommand.CloneAssignment, ProcessPackage);

            _handler3 = new Action<TcpPackage, int>[255];
            _handler3[(byte)TcpCommand.Authenticate] = ProcessPackage;
            _handler3[(byte)TcpCommand.Authenticated] = ProcessPackage;
            _handler3[(byte)TcpCommand.BadRequest] = ProcessPackage;
            _handler3[(byte)TcpCommand.ClientIdentified] = ProcessPackage;
            _handler3[(byte)TcpCommand.CloneAssignment] = ProcessPackage;

            //_packageMatcher
            var actionBuilder = new ActionMatchBuilder<TcpPackage, int>();
            actionBuilder.Match(_ => _.Command == TcpCommand.Authenticate, (p, c) => ProcessPackage(p, c));
            actionBuilder.Match(_ => _.Command == TcpCommand.Authenticated, (p, c) => ProcessPackage(p, c));
            actionBuilder.Match(_ => _.Command == TcpCommand.BadRequest, (p, c) => ProcessPackage(p, c));
            actionBuilder.Match(_ => _.Command == TcpCommand.ClientIdentified, (p, c) => ProcessPackage(p, c));
            actionBuilder.Match(_ => _.Command == TcpCommand.CloneAssignment, (p, c) => ProcessPackage(p, c));
            _packageMatcher = actionBuilder.Build();


            _matchBuilder = new MatchBuilder(CachedMatchCompiler<object>.Instance);
            _matchBuilder.Match(WrapAsyncHandler<MessageA>(ProcessMessageA));
            _matchBuilder.Match(WrapAsyncHandler<MessageB>(ProcessMessageB));
            _matchBuilder.Match(WrapAsyncHandler<MessageC>(ProcessMessageC));
            _matchBuilder.Match(WrapAsyncHandler<MessageD>(ProcessMessageD));
            _matchBuilder.Match(WrapAsyncHandler<MessageE>(ProcessMessageE));
            _matchBuilder.Match(WrapAsyncHandler<MessageF>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageG>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageH>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageI>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageJ>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageK>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageL>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageM>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageN>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageO>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageP>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageQ>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageR>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageS>(ProcessMessageAny));
            _matchBuilder.Match(WrapAsyncHandler<MessageT>(ProcessMessageAny));
            _partialReceive = _matchBuilder.Build();
        }

        private Action<T> WrapAsyncHandler<T>(Func<T, Task> asyncHandler)
        {
            return m => asyncHandler(m);
        }

        [Benchmark]
        public void AkkaMatchHandler()
        {
            if (!_partialReceive(_msgA)) { ThrowEx(); }
            if (!_partialReceive(_msgB)) { ThrowEx(); }
            if (!_partialReceive(_msgC)) { ThrowEx(); }
            if (!_partialReceive(_msgD)) { ThrowEx(); }
            if (!_partialReceive(_msgE)) { ThrowEx(); }
            if (!_partialReceive(_msgF)) { ThrowEx(); }
            if (!_partialReceive(_msgG)) { ThrowEx(); }
            if (!_partialReceive(_msgH)) { ThrowEx(); }
            if (!_partialReceive(_msgI)) { ThrowEx(); }
            if (!_partialReceive(_msgJ)) { ThrowEx(); }
            if (!_partialReceive(_msgK)) { ThrowEx(); }
            if (!_partialReceive(_msgL)) { ThrowEx(); }
            if (!_partialReceive(_msgM)) { ThrowEx(); }
            if (!_partialReceive(_msgN)) { ThrowEx(); }
            if (!_partialReceive(_msgO)) { ThrowEx(); }
            if (!_partialReceive(_msgP)) { ThrowEx(); }
            if (!_partialReceive(_msgQ)) { ThrowEx(); }
            if (!_partialReceive(_msgR)) { ThrowEx(); }
            if (!_partialReceive(_msgS)) { ThrowEx(); }
            if (!_partialReceive(_msgT)) { ThrowEx(); }
        }

        [Benchmark]
        public void EventStoreHandler()
        {
            _handlers.TryGetValue(_msgA.GetType(), out Func<Message, Task> handler);
            handler(_msgA);
            _handlers.TryGetValue(_msgB.GetType(), out handler);
            handler(_msgB);
            _handlers.TryGetValue(_msgC.GetType(), out handler);
            handler(_msgC);
            _handlers.TryGetValue(_msgD.GetType(), out handler);
            handler(_msgD);
            _handlers.TryGetValue(_msgE.GetType(), out handler);
            handler(_msgE);
            _handlers.TryGetValue(_msgF.GetType(), out handler);
            handler(_msgF);
            _handlers.TryGetValue(_msgG.GetType(), out handler);
            handler(_msgG);
            _handlers.TryGetValue(_msgH.GetType(), out handler);
            handler(_msgH);
            _handlers.TryGetValue(_msgI.GetType(), out handler);
            handler(_msgI);
            _handlers.TryGetValue(_msgJ.GetType(), out handler);
            handler(_msgJ);
            _handlers.TryGetValue(_msgK.GetType(), out handler);
            handler(_msgK);
            _handlers.TryGetValue(_msgL.GetType(), out handler);
            handler(_msgL);
            _handlers.TryGetValue(_msgM.GetType(), out handler);
            handler(_msgM);
            _handlers.TryGetValue(_msgN.GetType(), out handler);
            handler(_msgN);
            _handlers.TryGetValue(_msgO.GetType(), out handler);
            handler(_msgO);
            _handlers.TryGetValue(_msgP.GetType(), out handler);
            handler(_msgP);
            _handlers.TryGetValue(_msgQ.GetType(), out handler);
            handler(_msgQ);
            _handlers.TryGetValue(_msgR.GetType(), out handler);
            handler(_msgR);
            _handlers.TryGetValue(_msgS.GetType(), out handler);
            handler(_msgS);
            _handlers.TryGetValue(_msgT.GetType(), out handler);
            handler(_msgT);
        }

        [Benchmark]
        public void SimpleMatcher()
        {
            _binderMatcher(_msgA);
            _binderMatcher(_msgB);
            _binderMatcher(_msgC);
            _binderMatcher(_msgD);
            _binderMatcher(_msgE);
            _binderMatcher(_msgF);
            _binderMatcher(_msgG);
            _binderMatcher(_msgH);
            _binderMatcher(_msgI);
            _binderMatcher(_msgJ);
            _binderMatcher(_msgK);
            _binderMatcher(_msgL);
            _binderMatcher(_msgM);
            _binderMatcher(_msgN);
            _binderMatcher(_msgO);
            _binderMatcher(_msgP);
            _binderMatcher(_msgQ);
            _binderMatcher(_msgR);
            _binderMatcher(_msgS);
            _binderMatcher(_msgT);
        }

        [Benchmark]
        public void EsPackageHandler()
        {
            _handler2.TryGetValue(TcpCommand.Authenticate, out Action<TcpPackage, int> handler);
            handler(_packageA, 1);
            _handler2.TryGetValue(TcpCommand.Authenticated, out handler);
            handler(_packageB, 2);
            _handler2.TryGetValue(TcpCommand.BadRequest, out handler);
            handler(_packageC, 3);
            _handler2.TryGetValue(TcpCommand.ClientIdentified, out handler);
            handler(_packageD, 4);
            _handler2.TryGetValue(TcpCommand.CloneAssignment, out handler);
            handler(_packageE, 5);
        }

        [Benchmark]
        public void EsPackageHandler2()
        {
            _handler3[(byte)TcpCommand.Authenticate](_packageA, 1);
            _handler3[(byte)TcpCommand.Authenticated](_packageA, 1);
            _handler3[(byte)TcpCommand.BadRequest](_packageA, 1);
            _handler3[(byte)TcpCommand.ClientIdentified](_packageA, 1);
            _handler3[(byte)TcpCommand.CloneAssignment](_packageA, 1);
        }

        [Benchmark]
        public void ActionMatcher()
        {
            _packageMatcher(_packageA, 1);
            _packageMatcher(_packageB, 2);
            _packageMatcher(_packageC, 3);
            _packageMatcher(_packageD, 4);
            _packageMatcher(_packageE, 5);
        }

        private static void ThrowEx()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("No Handler");
            }
        }

        private static void ProcessPackage(TcpPackage package, int count)
        {
            var bts = package.AsByteArray();
            var p = bts.AsTcpPackage();
            //Console.WriteLine(count);
        }

        private static readonly IFormatterResolver s_resolver = MessagePackStandardResolver.Default;
        private static readonly IFormatterResolver s_typelessResolver = MessagePackStandardResolver.Typeless;
        internal static Task ProcessMessageA(MessageA msg)
        {
            //Console.WriteLine("a");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageA>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        internal static Task ProcessMessageB(MessageB msg)
        {
            //Console.WriteLine("b");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageB>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        internal static Task ProcessMessageC(MessageC msg)
        {
            //Console.WriteLine("c");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageC>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        internal static Task ProcessMessageD(MessageD msg)
        {
            //Console.WriteLine("d");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageD>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        internal static Task ProcessMessageE(MessageE msg)
        {
            //Console.WriteLine("e");
            var bts = MessagePackSerializer.Serialize(msg, s_resolver);
            var m = MessagePackSerializer.Deserialize<MessageE>(bts, s_resolver);
            return TaskConstants.Completed;
        }

        internal static Task ProcessMessageAny(Message msg)
        {
            //Console.WriteLine("e");
            var bts = MessagePackSerializer.Serialize<object>(msg, s_typelessResolver);
            var m = MessagePackSerializer.Deserialize<object>(bts, s_typelessResolver);
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

        [MessagePackObject]
        internal class MessageC : Message
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
        internal class MessageD : Message
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
        internal class MessageE : Message
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
        internal class MessageF : Message
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
        internal class MessageG : Message
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
        internal class MessageH : Message
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
        internal class MessageI : Message
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
        internal class MessageJ : Message
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
        internal class MessageK : Message
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
        internal class MessageL : Message
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
        internal class MessageM : Message
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
        internal class MessageN : Message
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
        internal class MessageO : Message
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
        internal class MessageP : Message
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
        internal class MessageQ : Message
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
        internal class MessageR : Message
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
        internal class MessageS : Message
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
        internal class MessageT : Message
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
