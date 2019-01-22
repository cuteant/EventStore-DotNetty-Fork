using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv.Native;
using EventStore.Transport.Tcp.Messages;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Tcp
{
    internal abstract class TcpHandlers : CommonHandlers
    {
        protected static readonly IFormatterResolver DefaultResolver = MessagePackStandardResolver.Default;

        private ITcpPackageListener _listener;
        private DotNettyConnection _connection;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void NotifyListener(TcpPackage msg) => _listener?.Notify(msg);

        protected TcpHandlers(DotNettyTransport transport)
            : base(transport)
        {
        }

        protected virtual void TrySetStatus(DotNettyConnection connection)
        {
            Interlocked.Exchange(ref _connection, connection);
        }

        protected override void RegisterListener(IChannel channel, ITcpPackageListener listener)
            => Interlocked.Exchange(ref this._listener, listener);

        protected override DotNettyConnection CreateConnection(IChannel channel)
            => new DotNettyConnection(Transport, channel);

        protected void NotifyDisassociated(in Disassociated disassociated)
        {
            //var connection = Volatile.Read(ref _connection);
            _connection.Close(disassociated);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            NotifyDisassociated(new Disassociated(DisassociateInfo.Unknown));
            base.ChannelInactive(context);
        }

        //public override void Read(IChannelHandlerContext context)
        //{
        //    //var connection = Volatile.Read(ref _connection);
        //    //_connection.NotifyReceiveStarting();
        //    base.Read(context);
        //}

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            //var connection = Volatile.Read(ref _connection);
            var buf = (IByteBuffer)message;
            var readableBytes = buf.ReadableBytes;
            _connection.NotifyReceiveCompleted(readableBytes);
            try
            {
                if (readableBytes > 0 && _listener != null)
                {
                    var bytes = buf.GetIoBuffer();

                    var packages = MessagePackSerializer.Deserialize<List<TcpPackage>>(bytes, DefaultResolver);
                    foreach (var package in packages)
                    {
                        _listener.Notify(package);
                    }
                }
                _connection.NotifyReceiveDispatched(readableBytes);
            }
            catch (Exception exc) { HandleBadRequest(exc); }
            finally
            {
                // decrease the reference count to 0 (releases buffer)
                buf.Release();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleBadRequest(Exception exc)
        {
            _connection.NotifyReceiveDispatched(0);
            _listener?.HandleBadRequest(new Disassociated(DisassociateInfo.CodecError, new ClosedConnectionException("Received bad network package. Error:", exc)));
        }

        /// <summary>TBD</summary>
        /// <param name="context">TBD</param>
        /// <param name="exception">TBD</param>
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            switch (exception)
            {
                case SocketException se:
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.Interrupted:
                        case SocketError.TimedOut:
                        case SocketError.OperationAborted:
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                            if (Logger.IsInformationLevelEnabled()) { Logger.DotNettyExceptionCaught(se, context); }
                            NotifyDisassociated(new Disassociated(DisassociateInfo.Shutdown, new ClosedConnectionException(se.Message, se)));
                            break;

                        default:
                            base.ExceptionCaught(context, exception);
                            NotifyDisassociated(new Disassociated(DisassociateInfo.Unknown, new ClosedConnectionException(exception.Message, exception)));
                            break;
                    }
                    break;

                // Libuv error handling: http://docs.libuv.org/en/v1.x/errors.html
                case ChannelException ce when (ce.InnerException is OperationException exc):
                    switch (exc.ErrorCode)
                    {
                        case ErrorCode.EINTR:        // interrupted system call
                        case ErrorCode.ENETDOWN:     // network is down
                        case ErrorCode.ENETUNREACH:  // network is unreachable
                        case ErrorCode.ENOTSOCK:     // socket operation on non-socket
                        case ErrorCode.ENOTSUP:      // operation not supported on socket
                        case ErrorCode.EPERM:        // operation not permitted
                        case ErrorCode.ETIMEDOUT:    // connection timed out
                        case ErrorCode.ECANCELED:    // operation canceled
                        case ErrorCode.ECONNABORTED: // software caused connection abort
                        case ErrorCode.ECONNRESET:   // connection reset by peer
                            if (Logger.IsInformationLevelEnabled()) { Logger.DotNettyExceptionCaught(exc, context); }
                            NotifyDisassociated(new Disassociated(DisassociateInfo.Shutdown, new ClosedConnectionException(exc.Description, exc)));
                            break;

                        default:
                            base.ExceptionCaught(context, exception);
                            NotifyDisassociated(new Disassociated(DisassociateInfo.Unknown, new ClosedConnectionException(exception.Message, exception)));
                            break;
                    }
                    break;

                default:
                    base.ExceptionCaught(context, exception);
                    NotifyDisassociated(new Disassociated(DisassociateInfo.Unknown, new ClosedConnectionException(exception.Message, exception)));
                    break;
            }

            context.CloseAsync().Ignore(); // close the channel
        }
    }
}
