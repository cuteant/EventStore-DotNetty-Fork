using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    /// <summary>Maintains a full duplex connection to Event Store.</summary>
    /// <remarks>An <see cref="IEventStoreConnection"/> operates differently than a SqlConnection. Normally
    /// when using an <see cref="IEventStoreConnection"/> you want to keep the connection open for a much longer of time than
    /// when you use a SqlConnection. If you prefer the usage pattern of using(new Connection()) .. then you would likely
    /// want to create a FlyWeight on top of the <see cref="IEventStoreConnection"/>.
    ///
    /// Another difference is that with the <see cref="IEventStoreConnection"/> all operations are handled in a full async manner
    /// (even if you call the synchronous behaviors). Many threads can use an <see cref="IEventStoreConnection"/> at the same
    /// time or a single thread can make many asynchronous requests. To get the best performance out of the connection
    /// it is generally recommended to use it in this way.</remarks>
    public interface IEventStoreConnectionBase : IDisposable
    {
        #region -- ConnectAsync --

        /// <summary>Connects the <see cref="IEventStoreConnection"/> asynchronously to a destination.</summary>
        /// <returns>A <see cref="Task"/> to wait upon.</returns>
        Task ConnectAsync();

        #endregion

        #region -- Close --

        /// <summary>Closes this <see cref="IEventStoreConnection"/>.</summary>
        void Close();

        #endregion

        #region -- DeleteStreamAsync --

        /// <summary>Deletes a stream from Event Store asynchronously.</summary>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;DeleteResult&gt;"/> containing the results of the delete stream operation.</returns>
        Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials = null);

        /// <summary>Deletes a stream from Event Store asynchronously.</summary>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
        /// <param name="hardDelete">Indicator for tombstoning vs soft-deleting the stream. Tombstoned streams can never be recreated.
        /// Soft-deleted streams can be written to again, but the EventNumber sequence will not start from 0.</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;DeleteResult&gt;"/> containing the results of the delete stream operation.</returns>
        Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null);

        #endregion

        #region -- AppendToStreamAsync --

        /// <summary>Appends events asynchronously to a stream.</summary>
        /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
        /// make a very large difference in the observed behavior. For example, if no stream exists
        /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
        ///
        /// There are also differences in idempotency between different types of calls.
        /// If you specify an ExpectedVersion aside from ExpectedVersion.Any, Event Store
        /// will give you an idempotency guarantee. If using ExpectedVersion.Any, Event Store
        /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
        /// <param name="stream">The name of the stream to append events to.</param>
        /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to.</param>
        /// <param name="evt">The event to append to the stream</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, EventData evt, UserCredentials userCredentials = null);

        /// <summary>Appends events asynchronously to a stream.</summary>
        /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
        /// make a very large difference in the observed behavior. For example, if no stream exists
        /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
        ///
        /// There are also differences in idempotency between different types of calls.
        /// If you specify an ExpectedVersion aside from ExpectedVersion.Any, Event Store
        /// will give you an idempotency guarantee. If using ExpectedVersion.Any, Event Store
        /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
        /// <param name="stream">The name of the stream to append events to.</param>
        /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to.</param>
        /// <param name="events">The events to append to the stream</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null);

        #endregion

        #region -- ConditionalAppendToStreamAsync --

        /// <summary>Appends events asynchronously to a stream if the stream version matches the <paramref name="expectedVersion"/>.</summary>
        /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
        /// make a very large difference in the observed behavior. For example, if no stream exists
        /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
        ///
        /// There are also differences in idempotency between different types of calls.
        /// If you specify an ExpectedVersion aside from ExpectedVersion.Any, Event Store
        /// will give you an idempotency guarantee. If using ExpectedVersion.Any, Event Store
        /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
        /// <param name="stream">The name of the stream to append events to.</param>
        /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to.</param>
        /// <param name="evt">The event to append to the stream</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;ConditionalWriteResult&gt;"/> describing if the operation succeeded and, if not, the reason for failure (which can be either stream version mismatch or trying to write to a deleted stream).</returns>
        Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, EventData evt, UserCredentials userCredentials = null);

        /// <summary>Appends events asynchronously to a stream if the stream version matches the <paramref name="expectedVersion"/>.</summary>
        /// <remarks>When appending events to a stream the <see cref="ExpectedVersion"/> choice can
        /// make a very large difference in the observed behavior. For example, if no stream exists
        /// and ExpectedVersion.Any is used, a new stream will be implicitly created when appending.
        ///
        /// There are also differences in idempotency between different types of calls.
        /// If you specify an ExpectedVersion aside from ExpectedVersion.Any, Event Store
        /// will give you an idempotency guarantee. If using ExpectedVersion.Any, Event Store
        /// will do its best to provide idempotency but does not guarantee idempotency.</remarks>
        /// <param name="stream">The name of the stream to append events to.</param>
        /// <param name="expectedVersion">The <see cref="ExpectedVersion"/> of the stream to append to.</param>
        /// <param name="events">The events to append to the stream</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;ConditionalWriteResult&gt;"/> describing if the operation succeeded and, if not, the reason for failure (which can be either stream version mismatch or trying to write to a deleted stream).</returns>
        Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null);

        #endregion

        #region -- StartTransactionAsync --

        /// <summary>Starts an asynchronous transaction in Event Store on a given stream.</summary>
        /// <remarks>A <see cref="EventStoreTransaction"/> allows the calling of multiple writes with multiple
        /// round trips over long periods of time between the caller and Event Store. This method
        /// is only available through the TCP interface and no equivalent exists for the RESTful interface.</remarks>
        /// <param name="stream">The stream to start a transaction on.</param>
        /// <param name="expectedVersion">The expected version of the stream at the time of starting the transaction</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;EventStoreTransaction&gt;"/> representing a multi-request transaction.</returns>
        Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion, UserCredentials userCredentials = null);

        #endregion

        #region -- Read event(s) --

        /// <summary>Asynchronously reads a single event from a stream.</summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="eventNumber">The event number to read, <see cref="StreamPosition">StreamPosition.End</see> to read the last event in the stream</param>
        /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation.</returns>
        Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null);

        /// <summary>Asynchronously reads count events from an Event Stream forwards (e.g. oldest to newest) starting from position start.</summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="start">The starting point to read from.</param>
        /// <param name="count">The count of items to read</param>
        /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
        Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

        /// <summary>Asynchronously reads count events from an Event Stream backwards (e.g. newest to oldest) from position asynchronously.</summary>
        /// <param name="stream">The Event Stream to read from.</param>
        /// <param name="start">The position to start reading from.</param>
        /// <param name="count">The count to read from the position</param>
        /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="Task&lt;StreamEventsSlice&gt;"/> containing the results of the read operation.</returns>
        Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null);

        #endregion

        #region -- SubscribeToStreamAsync --

        /// <summary>Asynchronously subscribes to a single event stream. New events
        /// written to the stream while the subscription is active will be pushed to the client.</summary>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when a new event is received over the subscription.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
        Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings, Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

        /// <summary>Asynchronously subscribes to a single event stream. New events
        /// written to the stream while the subscription is active will be pushed to the client.</summary>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;EventStoreSubscription&gt;"/> representing the subscription.</returns>
        Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

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
        /// <param name="eventAppeared">An action invoked when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Action<EventStoreStreamCatchUpSubscription, ResolvedEvent> eventAppeared, Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

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
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription.</param>
        /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription.</param>
        /// <returns>A <see cref="EventStoreStreamCatchUpSubscription"/> representing the subscription.</returns>
        EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
            Func<EventStoreStreamCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync, Action<EventStoreStreamCatchUpSubscription> liveProcessingStarted = null,
            Action<EventStoreStreamCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null, UserCredentials userCredentials = null);

        #endregion

        #region -- ConnectToPersistentSubscriptionAsync --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync. Many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
            ConnectToPersistentSubscriptionSettings settings,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null);

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync. Many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
            ConnectToPersistentSubscriptionSettings settings,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null);

        #endregion

        #region -- Create/Update/Delete PersistentSubscription --

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null);

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null);

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="stream">The name of the stream to delete the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null);

        #endregion

        #region -- StreamMetadata --

        /// <summary>Asynchronously sets the metadata for a stream.</summary>
        /// <param name="stream">The name of the stream for which to set metadata.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null);

        /// <summary>Asynchronously sets the metadata for a stream.</summary>
        /// <param name="stream">The name of the stream for which to set metadata.</param>
        /// <param name="expectedMetastreamVersion">The expected version for the write to the metadata stream.</param>
        /// <param name="metadata">A byte array representing the new metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;WriteResult&gt;"/> containing the results of the write operation.</returns>
        Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null);

        /// <summary>Asynchronously reads the metadata for a stream and converts the metadata into a <see cref="StreamMetadata"/>.</summary>
        /// <param name="stream">The name of the stream for which to read metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;StreamMetadataResult&gt;"/> representing system and user-specified metadata as properties.</returns>
        Task<StreamMetadataResult> GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null);

        /// <summary>Asynchronously reads the metadata for a stream as a byte array.</summary>
        /// <param name="stream">The name of the stream for which to read metadata.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task&lt;RawStreamMetadataResult&gt;"/> representing system metadata as properties and user-specified metadata as bytes.</returns>
        Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream, UserCredentials userCredentials = null);

        #endregion

        #region -- SetSystemSettingsAsync --

        /// <summary>Sets the global settings for the server or cluster to which the <see cref="IEventStoreConnection"/> is connected.</summary>
        /// <param name="settings">The <see cref="SystemSettings"/> to apply.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null);

        #endregion

        #region -- Properties --

        /// <summary>A <see cref="ConnectionSettings"/> object is an immutable representation of the settings for an <see cref="IEventStoreConnection"/>.</summary>
        ConnectionSettings Settings { get; }

        #endregion
    }
}
