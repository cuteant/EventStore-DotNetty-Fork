using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EventStore.Transport.Tcp
{
    partial class DotNettyConnection : IMonitoredTcpConnection
    {
        /// <summary>Address of the local endpoint</summary>
        public IPEndPoint LocalEndPoint { get; }

        /// <summary>Address of the remote endpoint</summary>
        public IPEndPoint RemoteEndPoint { get; }

        public bool IsInitialized => _channel.Open;
        public bool IsClosed
        {
            get => c_true == Volatile.Read(ref _closed);
            private set => Interlocked.Exchange(ref _closed, value ? c_true : c_false);
        }
        public bool InSend { get { return Interlocked.Read(ref _lastSendStarted) >= 0; } }
        public bool InReceive { get { return Interlocked.Read(ref _lastReceiveStarted) >= 0; } }
        public int PendingSendBytes { get { return _pendingSendBytes; } }
        public int InSendBytes { get { return _inSendBytes; } }
        public int PendingReceivedBytes { get { return _pendingReceivedBytes; } }
        public long TotalBytesSent { get { return Interlocked.Read(ref _totalBytesSent); } }
        public long TotalBytesReceived { get { return Interlocked.Read(ref _totalBytesReceived); } }
        public int SendCalls { get { return _sentAsyncs; } }
        public int SendCallbacks { get { return _sentAsyncCallbacks; } }
        public int ReceiveCalls { get { return _recvAsyncs; } }
        public int ReceiveCallbacks { get { return _recvAsyncCallbacks; } }

        public bool IsReadyForSend => _channel.Active;

        public bool IsReadyForReceive => _channel.Active;

        public bool IsFaulted => false;
        //{
        //    get
        //    {
        //        try
        //        {
        //            return !_isClosed && _socket.Poll(0, SelectMode.SelectError);
        //        }
        //        catch (ObjectDisposedException)
        //        {
        //            //TODO: why do we get this?
        //            return false;
        //        }
        //    }
        //}

        public DateTime? LastSendStarted
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastSendStarted);
                return ticks >= 0 ? new DateTime(ticks) : (DateTime?)null;
            }
        }

        public DateTime? LastReceiveStarted
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastReceiveStarted);
                return ticks >= 0 ? new DateTime(ticks) : (DateTime?)null;
            }
        }

        const int c_true = 1;
        const int c_false = 0;

        private long _lastSendStarted = -1;
        private long _lastReceiveStarted = -1;
        private int _closed;

        private int _pendingSendBytes;
        private int _inSendBytes;
        private int _pendingReceivedBytes;
        private long _totalBytesSent;
        private long _totalBytesReceived;

        private int _sentAsyncs;
        private int _sentAsyncCallbacks;
        private int _recvAsyncs;
        private int _recvAsyncCallbacks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifySendScheduled(int bytes)
        {
            Interlocked.Add(ref _pendingSendBytes, bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifySendStarting(int bytes)
        {
            if (Interlocked.CompareExchange(ref _lastSendStarted, DateTime.UtcNow.Ticks, -1) != -1)
                throw new Exception("Concurrent send detected.");
            Interlocked.Add(ref _pendingSendBytes, -bytes);
            Interlocked.Add(ref _inSendBytes, bytes);
            Interlocked.Increment(ref _sentAsyncs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifySendCompleted(int bytes)
        {
            Interlocked.Exchange(ref _lastSendStarted, -1);
            Interlocked.Add(ref _inSendBytes, -bytes);
            Interlocked.Add(ref _totalBytesSent, bytes);
            Interlocked.Increment(ref _sentAsyncCallbacks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void NotifyReceiveStarting()
        {
            if (Interlocked.CompareExchange(ref _lastReceiveStarted, DateTime.UtcNow.Ticks, -1) != -1)
                throw new Exception("Concurrent receive detected.");

            Interlocked.Increment(ref _recvAsyncs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void NotifyReceiveCompleted(int bytes)
        {
            Interlocked.Exchange(ref _lastReceiveStarted, -1);
            Interlocked.Add(ref _pendingReceivedBytes, bytes);
            Interlocked.Add(ref _totalBytesReceived, bytes);
            Interlocked.Increment(ref _recvAsyncCallbacks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void NotifyReceiveDispatched(int bytes)
        {
            Interlocked.Add(ref _pendingReceivedBytes, -bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyClosed()
        {
            IsClosed = true;
            TcpConnectionMonitor.Default.Unregister(this);
        }
    }
}
