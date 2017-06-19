using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  /// <summary>Maintains a full duplex connection to the EventStore.</summary>
  /// <remarks>An <see cref="IEventStoreConnection"/> operates quite differently than say a SqlConnection. Normally
  /// when using an <see cref="IEventStoreConnection"/> you want to keep the connection open for a much longer of time than
  /// when you use a SqlConnection. If you prefer the usage pattern of using(new Connection()) .. then you would likely
  /// want to create a FlyWeight on top of the <see cref="T:EventStore.ClientAPI.EventStoreConnection"/>.
  ///
  /// Another difference is that with the <see cref="IEventStoreConnection"/> all operations are handled in a full async manner
  /// (even if you call the synchronous behaviors). Many threads can use an <see cref="IEventStoreConnection"/> at the same
  /// time or a single thread can make many asynchronous requests. To get the most performance out of the connection
  /// it is generally recommended to use it in this way.</remarks>
  public interface IEventStoreConnection : IEventStoreConnectionBase
  {
    /// <summary>Gets the name of this connection. A connection name can be used for disambiguation in log files.</summary>
    string ConnectionName { get; }

    /// <summary>Continues transaction by provided transaction ID.</summary>
    /// <remarks>A <see cref="EventStoreTransaction"/> allows the calling of multiple writes with multiple
    /// round trips over long periods of time between the caller and the event store. This method
    /// is only available through the TCP interface and no equivalent exists for the RESTful interface.</remarks>
    /// <param name="transactionId">The transaction ID that needs to be continued.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="EventStoreTransaction"/> representing a multi-request transaction.</returns>
    EventStoreTransaction ContinueTransaction(long transactionId, UserCredentials userCredentials = null);

    #region -- Read all events --

    /// <summary>Reads All Events in the node forward asynchronously (e.g. beginning to end).</summary>
    /// <param name="position">The position to start reading from</param>
    /// <param name="maxCount">The maximum count to read</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;AllEventsSlice&gt;"/> containing the records read.</returns>
    Task<AllEventsSlice> ReadAllEventsForwardAsync(Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null);

    /// <summary>Reads All Events in the node backwards (e.g. end to beginning).</summary>
    /// <param name="position">The position to start reading from</param>
    /// <param name="maxCount">The maximum count to read</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;AllEventsSlice&gt;"/> containing the records read.</returns>
    Task<AllEventsSlice> ReadAllEventsBackwardAsync(Position position, int maxCount, bool resolveLinkTos, UserCredentials userCredentials = null);

    #endregion

    #region -- SubscribeToAllAsync --

    /// <summary>Asynchronously subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> SubscribeToAllAsync(SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    /// <summary>Asynchronously subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
    Task<EventStoreSubscription> SubscribeToAllAsync(SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    #endregion

    #region -- SubscribeToAllFrom --

    /// <summary>Subscribes to a all events. Existing events from lastCheckpoint
    /// onwards are read from the Event Store and presented to the user of
    /// <see cref="EventStoreAllCatchUpSubscription"/> as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreAllCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="lastCheckpoint">The position from which to start.
    ///
    /// To receive all events in the database, use <see cref="AllCheckpoint.AllStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the position representing the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="Position.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreAllCatchUpSubscription"/> representing the subscription.</returns>
    EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreAllCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreAllCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    /// <summary>Subscribes to a all events. Existing events from lastCheckpoint
    /// onwards are read from the Event Store and presented to the user of
    /// <see cref="EventStoreAllCatchUpSubscription"/> as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreAllCatchUpSubscription"/> switches from the reading
    /// phase to the live subscription phase.</summary>
    /// <param name="lastCheckpoint">The position from which to start.
    ///
    /// To receive all events in the database, use <see cref="AllCheckpoint.AllStart" />.
    /// If events have already been received and resubscription from the same point
    /// is desired, use the position representing the last event processed which
    /// appeared on the subscription.
    ///
    /// NOTE: Using <see cref="Position.Start" /> here will result in missing
    /// the first event in the stream.</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>A <see cref="EventStoreAllCatchUpSubscription"/> representing the subscription.</returns>
    EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreAllCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreAllCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null);

    #endregion

    #region -- Event handlers --

    /// <summary>Fired when an <see cref="IEventStoreConnection"/> connects to an Event Store server.</summary>
    event EventHandler<ClientConnectionEventArgs> Connected;

    /// <summary>Fired when an <see cref="IEventStoreConnection"/> is disconnected from an Event Store server
    /// by some means other than by calling the <see cref="IEventStoreConnectionBase.Close"/> method.</summary>
    event EventHandler<ClientConnectionEventArgs> Disconnected;

    /// <summary>Fired when an <see cref="IEventStoreConnection"/> is attempting to reconnect to an Event Store
    /// server following a disconnection.</summary>
    event EventHandler<ClientReconnectingEventArgs> Reconnecting;

    /// <summary>Fired when an <see cref="IEventStoreConnection"/> is closed either using the <see cref="IEventStoreConnectionBase.Close"/>
    /// method, or when reconnection limits are reached without a successful connection being established.</summary>
    event EventHandler<ClientClosedEventArgs> Closed;

    /// <summary>Fired when an error is thrown on an <see cref="IEventStoreConnection"/>.</summary>
    event EventHandler<ClientErrorEventArgs> ErrorOccurred;

    /// <summary>Fired when a client fails to authenticate to an Event Store server.</summary>
    event EventHandler<ClientAuthenticationFailedEventArgs> AuthenticationFailed;

    #endregion
  }
}
