using System;
using System.Threading.Tasks;

namespace EventStore.ClientAPI
{
  public interface IHandlerCollection : IHandlerRegistration
  {
    /// <summary>Retrieve a handler from the collection.
    /// If a matching handler cannot be found, the handler collection will either throw
    /// an <see cref="T:EventStore.ClientAPI.Exceptions.EventStoreHandlerException"/>, or return null, depending on the value of the
    /// ThrowOnNoMatchingHandler property.</summary>
    /// <typeparam name="TEvent">The type of handler to return</typeparam>
    /// <returns>The handler</returns>
    Func<IResolvedEvent<TEvent>, Task> GetHandler<TEvent>() where TEvent : class;

    /// <summary>Retrieve a handler from the collection.
    /// If a matching handler cannot be found, the handler collection will either throw
    /// an <see cref="T:EventStore.ClientAPI.Exceptions.EventStoreHandlerException"/>, or return null, depending on the value of the
    /// ThrowOnNoMatchingHandler property.</summary>
    /// <param name="messageType">The type of handler to return</param>
    /// <returns>The handler</returns>
    Func<IResolvedEvent2, Task> GetHandler(Type messageType);
  }
}