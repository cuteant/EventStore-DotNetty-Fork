using System;
using System.Collections.Generic;
using CuteAnt.Runtime;
using EventStore.ClientAPI.Common.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class SimpleQueuedHandler : AsynchQueueAgent<Message>, IDisposable
  {
    private readonly Dictionary<Type, Action<Message>> _handlers;

    public SimpleQueuedHandler()
    {
      _handlers = new Dictionary<Type, Action<Message>>();
    }

    public void RegisterHandler<T>(Action<T> handler) where T : Message
    {
      Ensure.NotNull(handler, nameof(handler));

      _handlers.Add(typeof(T), msg => handler((T)msg));
    }

    protected override void Process(Message message)
    {
      if (_handlers.TryGetValue(message.GetType(), out Action<Message> handler))
      {
        handler.Invoke(message);
      }
      else
      {
        var errorMsg = $"No handler registered for message {message.GetType().Name}";
        Log.LogCritical(errorMsg);
        throw new Exception(errorMsg);
      }
    }

    protected override void ProcessBatch(List<Message> messages)
    {
      throw new NotImplementedException();
    }

    #region IDisposable Members

    protected override void Dispose(bool disposing)
    {
      if (!disposing) return;

      Stop(dropPendingMessages: false);

      base.Dispose(disposing);
    }

    #endregion
  }
}