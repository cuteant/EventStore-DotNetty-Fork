using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI.Internal
{
    internal class SimpleQueuedHandler
    {
        private readonly Dictionary<Type, Func<Message, Task>> _handlers;
        private readonly ActionBlock<Message> _messageBlock;

        public SimpleQueuedHandler()
        {
            _handlers = new Dictionary<Type, Func<Message, Task>>();
            _messageBlock = new ActionBlock<Message>(ProcessMessageAsync,
                                                     new ExecutionDataflowBlockOptions { TaskScheduler = TaskUtility.TaskSchedulerPair.ExclusiveScheduler });
        }

        public void RegisterHandler<T>(Func<T, Task> handler)
          where T : Message
        {
            if (null == handler) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.handler); }

            _handlers.Add(typeof(T), async msg => await handler((T)msg).ConfigureAwait(false));
        }

        public void EnqueueMessage(Message message)
        {
            if (null == message) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            //_messageBlock.Post(message);
            AsyncContext.Run(s_sendToQueueFunc, _messageBlock, message);
        }

        private static readonly Func<ActionBlock<Message>, Message, Task<bool>> s_sendToQueueFunc = SendToQueueAsync;
        private static async Task<bool> SendToQueueAsync(ActionBlock<Message> targetBlock, Message message)
        {
            return await targetBlock.SendAsync(message).ConfigureAwait(false);
        }

        public Task EnqueueMessageAsync(Message message)
        {
            if (null == message) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            return _messageBlock.SendAsync(message);
        }

        private Task ProcessMessageAsync(Message message)
        {
            if (!_handlers.TryGetValue(message.GetType(), out Func<Message, Task> handler))
            {
                CoreThrowHelper.ThrowException_NoHandlerRegisteredForMessage(message);
            }
            return handler(message);
        }
    }
}