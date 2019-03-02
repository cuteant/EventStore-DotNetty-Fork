using System;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- SubscribeToStreamStart --

        /// <summary>Subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreStreamCatchUpSubscription SubscribeToStreamStart(this IEventStoreConnectionBase connection,
            string stream, CatchUpSubscriptionSettings settings,
            Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return connection.SubscribeToStreamFrom(stream, StreamPosition.Start, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreStreamCatchUpSubscription SubscribeToStreamStart(this IEventStoreConnectionBase connection,
            string stream, CatchUpSubscriptionSettings settings,
            Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return connection.SubscribeToStreamFrom(stream, StreamPosition.Start, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- SubscribeToStreamEnd --

        /// <summary>Subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="subscriptionSettings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreStreamCatchUpSubscription SubscribeToStreamEnd(this IEventStoreConnectionBase connection,
            string stream, CatchUpSubscriptionSettings subscriptionSettings,
            Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
              async (conn, streamId, settings, processingFunc, credentials)
                => await conn.SubscribeToStreamEndAsync(streamId, settings, processingFunc.Item1, processingFunc.Item2, processingFunc.Item3, credentials).ConfigureAwait(false),
              connection, stream, subscriptionSettings, Tuple.Create(eventAppeared, liveProcessingStarted, subscriptionDropped), userCredentials);
        }

        /// <summary>Subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="subscriptionSettings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreStreamCatchUpSubscription SubscribeToStreamEnd(this IEventStoreConnectionBase connection,
            string stream, CatchUpSubscriptionSettings subscriptionSettings,
            Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
              async (conn, streamId, settings, processingFunc, credentials)
                => await conn.SubscribeToStreamEndAsync(streamId, settings, processingFunc.Item1, processingFunc.Item2, processingFunc.Item3, credentials).ConfigureAwait(false),
              connection, stream, subscriptionSettings, Tuple.Create(eventAppearedAsync, liveProcessingStarted, subscriptionDropped), userCredentials);
        }

        #endregion

        #region -- SubscribeToStreamEndAsync --

        /// <summary>Asynchronously subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="Task&lt;EventStoreStreamCatchUpSubscription&gt;"/> representing the subscription.</returns>
        public static async Task<EventStoreStreamCatchUpSubscription> SubscribeToStreamEndAsync(this IEventStoreConnectionBase connection,
            string stream, CatchUpSubscriptionSettings settings,
            Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }

            long lastCheckpoint = StreamPosition.Start;
            var readResult = await ReadLastEventAsync(connection, stream, settings.ResolveLinkTos, userCredentials);
            if (EventReadStatus.Success == readResult.Status)
            {
                lastCheckpoint = readResult.EventNumber;
            }

            return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="Task&lt;EventStoreStreamCatchUpSubscription&gt;"/> representing the subscription.</returns>
        public static async Task<EventStoreStreamCatchUpSubscription> SubscribeToStreamEndAsync(this IEventStoreConnectionBase connection,
            string stream, CatchUpSubscriptionSettings settings,
            Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }

            long lastCheckpoint = StreamPosition.Start;
            var readResult = await ReadLastEventAsync(connection, stream, settings.ResolveLinkTos, userCredentials);
            if (EventReadStatus.Success == readResult.Status)
            {
                lastCheckpoint = readResult.EventNumber;
            }

            return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- SubscribeToStreamFrom --

        /// <summary>Subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        //[Obsolete("This method will be obsoleted in the next major version please switch to the overload with a settings object")]
        public static EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(this IEventStoreConnectionBase connection,
            string stream, long? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);

            return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a single event stream. Existing events from
        /// lastCheckpoint onwards are read from the stream
        /// and presented to the user of <see cref="EventStoreStreamCatchUpSubscription"/>
        /// as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreStreamCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(this IEventStoreConnectionBase connection,
            string stream, long? lastCheckpoint, bool resolveLinkTos,
            Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);

            return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- SubscribeToAllFrom --

        /// <summary>Subscribes to all events. Existing events from lastCheckpoint
        /// onwards are read from Event Store and presented to the user of
        /// <see cref="EventStoreAllCatchUpSubscription"/> as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreAllCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
        /// <param name="lastCheckpoint">The position from which to start.
        ///
        /// To receive all events in the database, use <see cref="AllCheckpoint.AllStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the position representing the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="Position.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreAllCatchUpSubscription"/> representing the subscription.</returns>
        //[Obsolete("This overload will be removed in the next major release please use the overload with a settings object")]
        public static EventStoreAllCatchUpSubscription SubscribeToAllFrom(this IEventStoreConnection connection,
            Position? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreAllCatchUpSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreAllCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return connection.SubscribeToAllFrom(lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to all events. Existing events from lastCheckpoint
        /// onwards are read from Event Store and presented to the user of
        /// <see cref="EventStoreAllCatchUpSubscription"/> as if they had been pushed.
        ///
        /// Once the end of the stream is read the subscription is
        /// transparently (to the user) switched to push new events as
        /// they are written.
        ///
        /// The action liveProcessingStarted is called when the
        /// <see cref="EventStoreAllCatchUpSubscription"/> switches from the reading
        /// phase to the live subscription phase.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
        /// <param name="lastCheckpoint">The position from which to start.
        ///
        /// To receive all events in the database, use <see cref="AllCheckpoint.AllStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the position representing the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="Position.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreAllCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreAllCatchUpSubscription SubscribeToAllFrom(this IEventStoreConnection connection,
            Position? lastCheckpoint, bool resolveLinkTos,
            Func<EventStoreAllCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreAllCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreAllCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return connection.SubscribeToAllFrom(lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription CatchUpSubscribe(this IEventStoreBus bus,
            string stream, long? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription CatchUpSubscribe(this IEventStoreBus bus,
            string stream, long? lastCheckpoint, bool resolveLinkTos,
            Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- CatchUpSubscribe(NonGeneric-Topic) --

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, bool resolveLinkTos,
            Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
            Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Action<EventStoreCatchUpSubscription, ResolvedEvent<object>> eventAppeared, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription, ResolvedEvent<object>, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- CatchUpSubscribe(Generic) --

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(this IEventStoreBus bus,
            long? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
            Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe<TEvent>(null, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(this IEventStoreBus bus,
            long? lastCheckpoint, bool resolveLinkTos,
            Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
            Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe<TEvent>(null, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(this IEventStoreBus bus,
            long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared, Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

            return bus.CatchUpSubscribe<TEvent>(null, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(this IEventStoreBus bus,
            long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync, Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

            return bus.CatchUpSubscribe<TEvent>(null, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(this IEventStoreBus bus,
            string topic, long? lastCheckpoint, bool resolveLinkTos,
            Action<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
            Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe<TEvent>(topic, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription&lt;TEvent&gt;"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription<TEvent> CatchUpSubscribe<TEvent>(this IEventStoreBus bus,
            string topic, long? lastCheckpoint, bool resolveLinkTos,
            Func<EventStoreCatchUpSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
            Action<EventStoreCatchUpSubscription<TEvent>> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe<TEvent>(topic, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, long? lastCheckpoint, bool resolveLinkTos, Action<IConsumerRegistration> addHandlers,
            Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream, lastCheckpoint, settings, _ => addHandlers(new HandlerAdder(_)), liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Action<IConsumerRegistration> addHandlers, Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return bus.CatchUpSubscribe(stream, lastCheckpoint, settings, _ => addHandlers(new HandlerAdder(_)), liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, long? lastCheckpoint, bool resolveLinkTos, Action<IHandlerRegistration> addHandlers,
            Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream, lastCheckpoint, settings, addHandlers, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- CatchUpSubscribe(Multi-Handler-Topic) --

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, bool resolveLinkTos, Action<IConsumerRegistration> addHandlers,
            Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, _ => addHandlers(new HandlerAdder(_)), liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Action<IConsumerRegistration> addHandlers, Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, _ => addHandlers(new HandlerAdder(_)), liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="resolveLinkTos">Whether to resolve Link events automatically.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="readBatchSize">The batch size to use during the read phase.</param>
        /// <param name="subscriptionName">The name of subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, bool resolveLinkTos, Action<IHandlerRegistration> addHandlers,
            Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, addHandlers, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

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
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="lastCheckpoint">The event number from which to start.
        ///
        /// To receive all events in the stream, use <see cref="StreamCheckpoint.StreamStart" />.
        /// If events have already been received and resubscription from the same point
        /// is desired, use the event number of the last event processed which
        /// appeared on the subscription.
        ///
        /// NOTE: Using <see cref="StreamPosition.Start" /> here will result in missing
        /// the first event in the stream.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreCatchUpSubscription"/> representing the subscription.</returns>
        public static EventStoreCatchUpSubscription2 CatchUpSubscribe(this IEventStoreBus bus,
            string stream, string topic, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Action<IHandlerRegistration> addHandlers, Action<EventStoreCatchUpSubscription2> liveProcessingStarted = null,
            Action<EventStoreCatchUpSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return bus.CatchUpSubscribe(stream.Combine(topic), lastCheckpoint, settings, addHandlers, liveProcessingStarted, subscriptionDropped, userCredentials);
        }

        #endregion
    }
}
