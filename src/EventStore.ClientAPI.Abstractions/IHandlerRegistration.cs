using System;
using System.Threading.Tasks;

namespace EventStore.ClientAPI
{
  // The idea of IHandlerRegistration is from EasyNetQ
  // https://github.com/EasyNetQ/EasyNetQ/blob/master/Source/EasyNetQ/Consumer/IHandlerRegistration.cs
  public interface IHandlerRegistration
  {
    /// <summary>Add an asynchronous handler</summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="handler">The handler</param>
    /// <returns></returns>
    IHandlerRegistration Add<TEvent>(Func<IResolvedEvent<TEvent>, Task> handler) where TEvent : class;

    /// <summary>Add a synchronous handler</summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="handler">The handler</param>
    /// <returns></returns>
    IHandlerRegistration Add<TEvent>(Action<IResolvedEvent<TEvent>> handler) where TEvent : class;

    /// <summary>Set to true if the handler collection should throw an <see cref="T:EventStore.ClientAPI.Exceptions.EventStoreHandlerException"/> 
    /// when no matching handler is found, or false if it should return a noop handler. Default is true.</summary>
    bool ThrowOnNoMatchingHandler { get; set; }
  }
}