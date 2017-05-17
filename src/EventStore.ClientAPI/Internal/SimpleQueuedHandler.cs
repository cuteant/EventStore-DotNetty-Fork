using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CuteAnt.Runtime;
using EventStore.ClientAPI.Common.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class SimpleQueuedHandler : AsynchAgent, IDisposable
  {
    private readonly Dictionary<Type, Action<Message>> _handlers;
    private BlockingCollection<Message> _messageQueue;

    public int Count => _messageQueue != null ? _messageQueue.Count : 0;

    public bool IsCompleted => _messageQueue != null ? _messageQueue.IsCompleted : true;

    public SimpleQueuedHandler()
    {
      _handlers = new Dictionary<Type, Action<Message>>();
      _messageQueue = new BlockingCollection<Message>();
    }

    public void RegisterHandler<T>(Action<T> handler) where T : Message
    {
      Ensure.NotNull(handler, nameof(handler));

      _handlers.Add(typeof(T), msg => handler((T)msg));
    }

    public void EnqueueMessage(Message message)
    {
      if (null == _messageQueue) { return; }
      if (null == message) { throw new ArgumentNullException(nameof(message)); }
      _messageQueue.Add(message);
    }

    protected sealed override void Run()
    {
      while (true)
      {
        if (null == Cts || Cts.IsCancellationRequested) { return; }
        Message message;
        try
        {
          message = _messageQueue.Take();
        }
        catch (InvalidOperationException)
        {
          Log.LogWarning("Stop message processed");
          break;
        }

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
    }

    public sealed override void Stop()
    {
      if (_messageQueue != null)
      {
        _messageQueue.CompleteAdding();
        var spinWait = new SpinWait();
        while (true)
        {
          spinWait.SpinOnce();
          if (this.IsCompleted) { break; }
        }
      }
      base.Stop();
    }

    #region IDisposable Members

    protected override void Dispose(bool disposing)
    {
      if (!disposing) return;

      base.Dispose(disposing);

      Stop();

      _messageQueue?.Dispose();
      _messageQueue = null;
    }

    #endregion
  }
}