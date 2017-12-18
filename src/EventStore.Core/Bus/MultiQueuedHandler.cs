﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Common.Utils;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;

namespace EventStore.Core.Bus
{
    public class MultiQueuedHandler : IHandle<Message>, IPublisher, IThreadSafePublisher
    {
        public readonly IQueuedHandler[] Queues;

        private readonly Func<Message, int> _queueHash;
        private int _nextQueueNum = -1;

        public MultiQueuedHandler(int queueCount,
                                     Func<int, IQueuedHandler> queueFactory,
                                     Func<Message, int> queueHash = null)
        {
            Ensure.Positive(queueCount, nameof(queueCount));
            Ensure.NotNull(queueFactory, nameof(queueFactory));

            Queues = new IQueuedHandler[queueCount];
            for (int i = 0; i < Queues.Length; ++i)
            {
                Queues[i] = queueFactory(i);
            }
            //TODO AN remove _queueHash function
            _queueHash = queueHash ?? NextQueueHash;
        }

        public MultiQueuedHandler(params QueuedHandler[] queues) : this(queues, null)
        {
            Ensure.Positive(queues.Length, "queues.Length");
        }

        public MultiQueuedHandler(IQueuedHandler[] queues, Func<Message, int> queueHash)
        {
            Ensure.NotNull(queues, nameof(queues));
            Ensure.Positive(queues.Length, "queues.Length");

            Queues = queues;
            _queueHash = queueHash ?? NextQueueHash;
        }

        private int NextQueueHash(Message msg)
        {
            return Interlocked.Increment(ref _nextQueueNum);
        }

        public void Start()
        {
            for (int i = 0; i < Queues.Length; ++i)
            {
                Queues[i].Start();
            }
        }

        public void Stop()
        {
            var stopTasks = new Task[Queues.Length];
            for (int i = 0; i < Queues.Length; ++i)
            {
                int queueNum = i;
                stopTasks[i] = Task.Factory.StartNew(handler => handler.Stop(), Queues[queueNum], CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }
            Task.WaitAll(stopTasks);
        }

        public void Handle(Message message)
        {
            Publish(message);
        }

        public void Publish(Message message)
        {
            var affineMsg = message as IQueueAffineMessage;
            int queueHash = affineMsg != null ? affineMsg.QueueId : _queueHash(message);
            var queueNum = (int)((uint)queueHash % Queues.Length);
            Queues[queueNum].Publish(message);
        }

        public void PublishToAll(Message message)
        {
            for (int i = 0; i < Queues.Length; ++i)
            {
                Queues[i].Publish(message);
            }
        }
    }
}