using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.Exceptions;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
  // The idea of DefaultHandlerCollection is from EasyNetQ
  // https://github.com/EasyNetQ/EasyNetQ/blob/master/Source/EasyNetQ/Consumer/HandlerCollection.cs
  internal sealed class DefaultHandlerCollection : IHandlerCollection
  {
    private static readonly Func<IResolvedEvent2, Task> s_emptyHandler = iEvent => TaskConstants.Completed;

    private readonly object _thisLock = new object();
    private readonly IDictionary<Type, Func<IResolvedEvent2, Task>> _handlers;
    private readonly HashSet<Type> _noMatching;

    private static readonly ILogger s_logger = TraceLogger.GetLogger<DefaultHandlerCollection>();
    private bool _throwOnNoMatchingHandler;

    public DefaultHandlerCollection(bool throwOnNoMatchingHandler = false)
    {
      _throwOnNoMatchingHandler = throwOnNoMatchingHandler;
      _handlers = new Dictionary<Type, Func<IResolvedEvent2, Task>>();
      _noMatching = new HashSet<Type>();
    }

    public IHandlerRegistration Add<TEvent>(Func<IResolvedEvent<TEvent>, Task> handler) where TEvent : class
    {
      if (null == handler) { throw new ArgumentNullException(nameof(handler)); }

      if (_handlers.ContainsKey(typeof(TEvent)))
      {
        throw new EventStoreHandlerException($"There is already a handler for event type '{typeof(TEvent).Name}'");
      }

      _handlers.Add(typeof(TEvent), (iEvent) => handler((IResolvedEvent<TEvent>)iEvent));
      return this;
    }

    public IHandlerRegistration Add<TEvent>(Action<IResolvedEvent<TEvent>> handler) where TEvent : class
    {
      if (null == handler) { throw new ArgumentNullException(nameof(handler)); }

      Add<TEvent>(async iEvent =>
      {
        handler(iEvent);
        await TaskConstants.Completed;
      });
      return this;
    }

    public Func<IResolvedEvent<TEvent>, Task> GetHandler<TEvent>() where TEvent : class
    {
      return GetHandler(typeof(TEvent));
    }

    public Func<IResolvedEvent2, Task> GetHandler(Type eventType)
    {
      if (_handlers.TryGetValue(eventType, out Func<IResolvedEvent2, Task> func)) { return func; }

      if (!_throwOnNoMatchingHandler)
      {
        lock (_thisLock)
        {
          var hander = GetHandlerLocal(eventType);
          if (hander != null) { return hander; }
          _handlers.Add(eventType, s_emptyHandler);
          if (s_logger.IsInformationLevelEnabled())
          {
            s_logger.LogInformation($"No handler found for event type {eventType.Name}, the default hander has been used.");
          }
          return s_emptyHandler;
        }
      }
      else
      {
        if (!_noMatching.Contains(eventType))
        {
          lock (_thisLock)
          {
            var hander = GetHandlerLocal(eventType);
            if (hander != null) { return hander; }
            _noMatching.Add(eventType);
          }
        }
        var msg = $"No handler found for event type {eventType.Name}";
        s_logger.LogError(msg);
        throw new EventStoreHandlerException(msg);
      }

      Func<IResolvedEvent2, Task> GetHandlerLocal(Type eType)
      {
        if (_handlers.TryGetValue(eventType, out func)) { return func; }

        // no exact handler match found, so let's see if we can find a handler that
        // handles a supertype of the consumed event.
        var handlerType = _handlers.Keys.FirstOrDefault(type => type.IsAssignableFrom(eventType));
        if (handlerType != null)
        {
          var hander = _handlers[handlerType];
          _handlers.Add(eventType, hander);
          return hander;
        }
        return null;
      }
    }

    public bool ThrowOnNoMatchingHandler { get => _throwOnNoMatchingHandler; set => _throwOnNoMatchingHandler = value; }
  }
}