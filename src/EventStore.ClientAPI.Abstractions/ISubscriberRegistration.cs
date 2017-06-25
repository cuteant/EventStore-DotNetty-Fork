using System;
using System.Threading.Tasks;

namespace EventStore.ClientAPI
{
  // The idea of ISubscriberRegistration is from EasyNetQ
  // https://github.com/EasyNetQ/EasyNetQ/blob/master/Source/EasyNetQ/Consumer/IReceiveRegistration.cs
  public interface ISubscriberRegistration
  {
    /// <summary>Add an asychronous message handler to this receiver</summary>
    /// <typeparam name="TEvent">The type of event to receive</typeparam>
    /// <param name="eventAppearedAsync">The event handler</param>
    /// <returns>'this' for fluent configuration</returns>
    ISubscriberRegistration Add<TEvent>(Func<TEvent, Task> eventAppearedAsync) where TEvent : class;

    /// <summary>Add a message handler to this receiver</summary>
    /// <typeparam name="TEvent">The type of event to receive</typeparam>
    /// <param name="eventAppeared">The event handler</param>
    /// <returns>'this' for fluent configuration</returns>
    ISubscriberRegistration Add<TEvent>(Action<TEvent> eventAppeared) where TEvent : class;
  }
}