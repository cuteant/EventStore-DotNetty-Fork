using System;
using System.ComponentModel;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  public interface IEventStoreConnectionBase2 : IEventStoreConnectionBase
  {
    #region -- GetEventAsync --

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    Task<EventReadResult<object>> GetEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Asynchronously reads a single event from a stream.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
    Task<EventReadResult<TEvent>> GetEventAsync<TEvent>(string topic, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class;

    #endregion

    #region -- GetStreamEventsForwardAsync --

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<object>> GetStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<TEvent>> GetStreamEventsForwardAsync<TEvent>(string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class;

    #endregion

    #region -- GetStreamEventsBackwardAsync --

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="stream">The Event Stream to read from</param>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<object>> GetStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="start">The position to start reading from</param>
    /// <param name="count">The count to read from the position</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
    Task<StreamEventsSlice<TEvent>> GetStreamEventsBackwardAsync<TEvent>(string topic, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class;

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

    #region -- VolatileSubscribeAsync(Multi-Handler) --

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="addHandlers">A function to add handlers to the consumer</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings, Action<IHandlerRegistration> addHandlers,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

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


    #region -- CatchUpSubscribe(NonGeneric) --

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
    EventStoreCatchUpSubscription CatchUpSubscribe(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
    EventStoreCatchUpSubscription CatchUpSubscribe(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

    #endregion

    #region -- CatchUpSubscribe(Multi-Handler) --

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="addHandlers">A function to add handlers to the consumer</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
    EventStoreCatchUpSubscription2 CatchUpSubscribe(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<IHandlerRegistration> addHandlers, Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

    #endregion

    #region -- CatchUpSubscribe(Generic-Topic) --

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
    EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared, Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
      where TEvent : class;

    /// <summary>Subscribes to a single event stream. Existing events from
    /// lastCheckpoint onwards are read from the stream
    /// and presented to the user of <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/>
    /// as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="lastCheckpoint">The event number from which to start.
    ///
    /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the event number of the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
    EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
      where TEvent : class;

    #endregion


    #region -- Create/Update/Delete PersistentSubscription --

    /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="credentials">The credentials to be used for this operation.</param>
    /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
    Task UpdatePersistentSubscriptionAsync<TEvent>(string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null) where TEvent : class;

    /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="credentials">The credentials to be used for this operation.</param>
    /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
    Task CreatePersistentSubscriptionAsync<TEvent>(string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null) where TEvent : class;

    /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="groupName">The name of the group to delete</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
    Task DeletePersistentSubscriptionAsync<TEvent>(string topic, string groupName, UserCredentials userCredentials = null) where TEvent : class;

    #endregion

    #region -- PersistentSubscribeAsync(NonGeneric) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStorePersistentSubscription> PersistentSubscribeAsync(string stream, string subscriptionId,
      ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStorePersistentSubscription> PersistentSubscribeAsync(string stream, string subscriptionId,
      ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    #endregion

    #region -- PersistentSubscribeAsync(Multi-Handler) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="addHandlers">A function to add handlers to the consumer</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
    Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(string stream, string subscriptionId,
      ConnectToPersistentSubscriptionSettings settings, Action<IHandlerRegistration> addHandlers,
      Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    #endregion

    #region -- PersistentSubscribeAsync(Generic-Topic) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="topic">The topic</param>
    /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(string topic, string subscriptionId,
      ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class;

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="topic">The topic</param>
    /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(string topic, string subscriptionId,
      ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class;

    #endregion


    #region -- Internal --

    /// <summary>Reads count Events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="start">The starting point to read from</param>
    /// <param name="count">The count of items to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;StreamEventsSlice2&gt;"/> containing the results of the read operation.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    Task<StreamEventsSlice2> InternalGetStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    Task<EventStoreSubscription> InternalVolatileSubscribeAsync(string stream, SubscriptionSettings settings, Func<EventStoreSubscription, IResolvedEvent2, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

    #endregion
  }
}
