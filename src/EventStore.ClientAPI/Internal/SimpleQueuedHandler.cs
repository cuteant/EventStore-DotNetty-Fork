using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI.Internal
{
  internal class SimpleQueuedHandler
  {
    private readonly Dictionary<Type, Action<Message>> _handlers;
    private readonly ActionBlock<Message> _messageBlock;

    public SimpleQueuedHandler()
    {
      _handlers = new Dictionary<Type, Action<Message>>();
      _messageBlock = new ActionBlock<Message>(new Action<Message>(ProcessMessage),
                                               new ExecutionDataflowBlockOptions { TaskScheduler = TaskUtility.TaskSchedulerPair.ExclusiveScheduler });
    }

    public void RegisterHandler<T>(Action<T> handler)
      where T : Message
    {
      Ensure.NotNull(handler, nameof(handler));

      _handlers.Add(typeof(T), msg => handler((T)msg));
    }

    public void EnqueueMessage(Message message)
    {
      Ensure.NotNull(message, nameof(message));
      _messageBlock.Post(message);
    }

    private void ProcessMessage(Message message)
    {
      if (!_handlers.TryGetValue(message.GetType(), out Action<Message> handler))
      {
        throw new Exception($"No handler registered for message {message.GetType().Name}");
      }
      handler.Invoke(message);
    }
  }
}