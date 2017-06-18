using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  public interface IEventStoreConnectionBase2 : IEventStoreConnectionBase
  {
    #region -- Get event(s) --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    Task<EventReadResult<object>> GetEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<object>> GetStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="stream">The Event Stream to read from</param>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<object>> GetStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    Task<EventReadResult<TEvent>> GetEventAsync<TEvent>(long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class;

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<TEvent>> GetStreamEventsForwardAsync<TEvent>(long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class;

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<TEvent>> GetStreamEventsBackwardAsync<TEvent>(long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class;

    #endregion

    #region -- VolatileSubscribeAsync(NonGeneric) --

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings, Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings, Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

    #endregion

    #region -- VolatileSubscribeAsync(Generic) --

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(SubscriptionSettings settings, Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null) where TEvent : class;

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(SubscriptionSettings settings, Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null) where TEvent : class;

    #endregion

    #region -- VolatileSubscribeAsync(Generic-Topic) --

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(string topic, SubscriptionSettings settings, Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null) where TEvent : class;

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(string topic, SubscriptionSettings settings, Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null) where TEvent : class;

    #endregion
  }
}
