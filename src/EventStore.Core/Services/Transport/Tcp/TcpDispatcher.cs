using System;
using System.Security.Principal;
using EventStore.Common.Utils;
using EventStore.Core.Messaging;
using EventStore.Transport.Tcp.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Transport.Tcp
{
    public abstract class TcpDispatcher: ITcpDispatcher
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<TcpDispatcher>();

        private readonly Func<TcpPackage, IEnvelope, IPrincipal, string, string, TcpConnectionManager, Message>[][] _unwrappers;
        private readonly int _lastVersion;
        private readonly SimpleMatchBuilder<Message, TcpPackage>[] _wrapperBuilders;
        private readonly Func<Message, TcpPackage>[] _wrapperFuncs;

        protected TcpDispatcher()
        {
            _unwrappers = new Func<TcpPackage, IEnvelope, IPrincipal, string, string, TcpConnectionManager, Message>[2][];
            _unwrappers[0] = new Func<TcpPackage, IEnvelope, IPrincipal, string, string, TcpConnectionManager, Message>[255];
            _unwrappers[1] = new Func<TcpPackage, IEnvelope, IPrincipal, string, string, TcpConnectionManager, Message>[255];

            _wrapperFuncs = new Func<Message, TcpPackage>[2];
            _wrapperBuilders = new SimpleMatchBuilder<Message, TcpPackage>[2];
            _wrapperBuilders[0] = new SimpleMatchBuilder<Message, TcpPackage>();
            _wrapperBuilders[1] = new SimpleMatchBuilder<Message, TcpPackage>();
            _lastVersion = _wrapperFuncs.Length - 1;
        }

        public virtual ITcpDispatcher Build()
        {
            _wrapperFuncs[0] = _wrapperBuilders[0].Build();
            _wrapperFuncs[1] = _wrapperBuilders[1].Build();
            return this;
        }

        protected void AddWrapper<T>(Func<T, TcpPackage> wrapper, ClientVersion version) where T : Message
        {
            _wrapperBuilders[(byte)version].Match(handler: wrapper);
        }

        protected void AddUnwrapper<T>(TcpCommand command, Func<TcpPackage, IEnvelope, T> unwrapper, ClientVersion version) where T : Message
        {
            _unwrappers[(byte)version][(byte)command] = (pkg, env, user, login, pass, conn) => unwrapper(pkg, env);
        }

        protected void AddUnwrapper<T>(TcpCommand command, Func<TcpPackage, IEnvelope, TcpConnectionManager, T> unwrapper, ClientVersion version) where T : Message
        {
            _unwrappers[(byte)version][(byte)command] = (pkg, env, user, login, pass, conn) => unwrapper(pkg, env, conn);
        }

        protected void AddUnwrapper<T>(TcpCommand command, Func<TcpPackage, IEnvelope, IPrincipal, T> unwrapper, ClientVersion version) where T : Message
        {
            _unwrappers[(byte)version][(byte)command] = (pkg, env, user, login, pass, conn) => unwrapper(pkg, env, user);
        }

        protected void AddUnwrapper<T>(TcpCommand command, Func<TcpPackage, IEnvelope, IPrincipal, string, string, T> unwrapper, ClientVersion version) where T : Message
        {
            _unwrappers[(byte)version][(byte)command] = (pkg, env, user, login, pass, conn) => unwrapper(pkg, env, user, login, pass);
        }

        protected void AddUnwrapper<T>(TcpCommand command, Func<TcpPackage, IEnvelope, IPrincipal, string, string, TcpConnectionManager, T> unwrapper, ClientVersion version) 
            where T : Message
        {
            _unwrappers[(byte)version][(byte) command] = (Func<TcpPackage, IEnvelope, IPrincipal, string, string, TcpConnectionManager, Message>) unwrapper;
        }

        public TcpPackage WrapMessage(Message message, byte version)
        {
            if (message == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message);

            try
            {
                return _wrapperFuncs[version](message);
            }
            catch (MatchException)
            {
                try
                {
                    return _wrapperFuncs[_lastVersion](message);
                }
                catch (MatchException) { }
                catch (Exception e) { Log.ErrorWhileWrappingMessage(message, e); }
            }
            catch (Exception exc)
            {
                Log.ErrorWhileWrappingMessage(message, exc);
            }
            return null;
        }

        public Message UnwrapPackage(TcpPackage package, IEnvelope envelope, IPrincipal user, string login, string pass, TcpConnectionManager connection, byte version)
        {
            if (envelope == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.envelope);

            var unwrapper = _unwrappers[version][(byte)package.Command];
            if (unwrapper == null) 
            {
                unwrapper = _unwrappers[_unwrappers.Length - 1][(byte)package.Command];
            }
            if (unwrapper != null)
            {
                try
                {
                    return unwrapper(package, envelope, user, login, pass, connection);
                }
                catch (Exception exc)
                {
                    Log.ErrorWhileUnwrappingTcppackageWithCommand(package.Command, exc);
                }
            }
            return null;
        }
    }
}