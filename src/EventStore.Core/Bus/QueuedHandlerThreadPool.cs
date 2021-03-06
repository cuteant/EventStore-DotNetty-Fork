﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Monitoring.Stats;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Bus
{
    /// <summary>Lightweight in-memory queue with a separate thread in which it passes messages
    /// to the consumer. It also tracks statistics about the message processing to help
    /// in identifying bottlenecks</summary>
    public class QueuedHandlerThreadPool : IQueuedHandler, IMonitoredQueue, IThreadSafePublisher
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<QueuedHandlerThreadPool>();

        public int MessageCount { get { return _queue.Count; } }
        public string Name { get { return _queueStats.Name; } }

        private readonly IHandle<Message> _consumer;

        private readonly bool _watchSlowMsg;
        private readonly TimeSpan _slowMsgThreshold;

        private readonly ConcurrentQueue<Message> _queue = new ConcurrentQueue<Message>();

        private volatile bool _stop;
        private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(true);
        private readonly TimeSpan _threadStopWaitTimeout;

        // monitoring
        private readonly QueueMonitor _queueMonitor;
        private readonly QueueStatsCollector _queueStats;

        private int _isRunning;
        private int _queueStatsState; //0 - never started, 1 - started, 2 - stopped

        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();


        public QueuedHandlerThreadPool(IHandle<Message> consumer,
                                       string name,
                                       bool watchSlowMsg = true,
                                       TimeSpan? slowMsgThreshold = null,
                                       TimeSpan? threadStopWaitTimeout = null,
                                       string groupName = null)
        {
            if (consumer is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.consumer); }
            if (name is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name); }

            _consumer = consumer;

            _watchSlowMsg = watchSlowMsg;
            _slowMsgThreshold = slowMsgThreshold ?? InMemoryBus.DefaultSlowMessageThreshold;
            _threadStopWaitTimeout = threadStopWaitTimeout ?? QueuedHandler.DefaultStopWaitTimeout;

            _queueMonitor = QueueMonitor.Default;
            _queueStats = new QueueStatsCollector(name, groupName);
        }

        public Task Start()
        {
            _queueMonitor.Register(this);
            return _tcs.Task;
        }

        public void Stop()
        {
            _stop = true;
            if (!_stopped.Wait(_threadStopWaitTimeout))
            {
                ThrowHelper.ThrowTimeoutException_UnableToStopThread(Name);
            }
            TryStopQueueStats();
            _queueMonitor.Unregister(this);
        }

        public void RequestStop()
        {
            _stop = true;
            TryStopQueueStats();
            _queueMonitor.Unregister(this);
        }

        private void TryStopQueueStats()
        {
            if (0u >= (uint)Interlocked.CompareExchange(ref _isRunning, 1, 0))
            {
                if (Interlocked.CompareExchange(ref _queueStatsState, 2, 1) == 1)
                    _queueStats.Stop();
                Interlocked.CompareExchange(ref _isRunning, 0, 1);
            }
        }

        private void ReadFromQueue(object o)
        {
            try
            {
                if (0u >= (uint)Interlocked.CompareExchange(ref _queueStatsState, 1, 0))
                    _queueStats.Start();

                var proceed = true;
#if DEBUG
                var traceEnabled = Log.IsTraceLevelEnabled();
#endif

                while (proceed)
                {
                    _stopped.Reset();
                    _queueStats.EnterBusy();

                    while (!_stop && _queue.TryDequeue(out Message msg))
                    {
#if DEBUG
                        _queueStats.Dequeued(msg);
#endif
                        try
                        {
                            var queueCnt = _queue.Count;
                            _queueStats.ProcessingStarted(msg.GetType(), queueCnt);

                            if (_watchSlowMsg)
                            {
                                var start = DateTime.UtcNow;

                                _consumer.Handle(msg);

                                var elapsed = DateTime.UtcNow - start;
                                if (elapsed > _slowMsgThreshold)
                                {
#if DEBUG
                                    if (traceEnabled) { Log.ShowQueueMsg(_queueStats, (int)elapsed.TotalMilliseconds, queueCnt, _queue.Count); }
#endif
                                    if (elapsed > QueuedHandler.VerySlowMsgThreshold && !(msg is SystemMessage.SystemInit))
                                    {
                                        Log.VerySlowQueueMsg(_queueStats, (int)elapsed.TotalMilliseconds, queueCnt, _queue.Count);
                                    }
                                }
                            }
                            else
                            {
                                _consumer.Handle(msg);
                            }

                            _queueStats.ProcessingEnded(1);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorWhileProcessingMessageInQueuedHandler(msg, _queueStats.Name, ex);
#if DEBUG
                            throw;
#endif
                        }
                    }

                    _queueStats.EnterIdle();
                    Interlocked.CompareExchange(ref _isRunning, 0, 1);
                    if (_stop)
                    {
                        TryStopQueueStats();
                    }

                    _stopped.Set();

                    // try to reacquire lock if needed
                    proceed = !_stop && (uint)_queue.Count > 0u && 0u >= (uint)Interlocked.CompareExchange(ref _isRunning, 1, 0);
                }
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
                throw;
            }

        }

        public void Publish(Message message)
        {
            //if (message is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
#if DEBUG
            _queueStats.Enqueued();
#endif
            _queue.Enqueue(message);
            if (!_stop && 0u >= (uint)Interlocked.CompareExchange(ref _isRunning, 1, 0))
            {
                ThreadPoolScheduler.Schedule(ReadFromQueue, (object)null);
            }
        }

        public void Handle(Message message)
        {
            Publish(message);
        }

        public QueueStats GetStatistics()
        {
            return _queueStats.GetStatistics(_queue.Count);
        }
    }
}

