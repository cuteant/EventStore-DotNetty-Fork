using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI.Internal
{
    internal class SimpleQueuedHandler
    {
        private ActionBlock<Message> _messageBlock;
        private readonly SimpleMatchBuilder<Message, Task> _matchBuilder;

        public SimpleQueuedHandler()
        {
            _matchBuilder = new SimpleMatchBuilder<Message, Task>();
        }

        public void RegisterHandler<T>(Func<T, Task> handler)
          where T : Message
        {
            if (null == handler) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.handler); }
            _matchBuilder.Match(handler: handler);
        }

        public void Build()
        {
            var processMessageAsync = _matchBuilder.Build();
            _messageBlock = new ActionBlock<Message>(processMessageAsync,
                                                     new ExecutionDataflowBlockOptions { TaskScheduler = TaskUtility.TaskSchedulerPair.ExclusiveScheduler });
        }

        public void EnqueueMessage(Message message)
        {
            if (null == message) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            _messageBlock.Post(message);
            //AsyncContext.Run(s_sendToQueueFunc, _messageBlock, message);
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
    }
}