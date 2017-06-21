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
      Ensure.NotNull(handler, nameof(handler));

      _handlers.Add(typeof(T), async msg => await handler((T)msg).ConfigureAwait(false));
    }

    public void EnqueueMessage(Message message)
    {
      Ensure.NotNull(message, nameof(message));
      //_messageBlock.Post(message);
      AsyncContext.Run(async (targetBlock, msg) => await targetBlock.SendAsync(msg).ConfigureAwait(false), _messageBlock, message);
    }

    public Task EnqueueMessageAsync(Message message)
    {
      Ensure.NotNull(message, nameof(message));
      return _messageBlock.SendAsync(message);
    }

    private Task ProcessMessageAsync(Message message)
    {
      if (!_handlers.TryGetValue(message.GetType(), out Func<Message, Task> handler))
      {
        throw new Exception($"No handler registered for message {message.GetType().Name}");
      }
      return handler(message);
    }
  }
}