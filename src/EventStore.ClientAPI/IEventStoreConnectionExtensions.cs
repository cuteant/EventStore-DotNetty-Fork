using System;
using System.Globalization;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  public static class IEventStoreConnectionExtensions
  {
    #region -- CreatePersistentSubscription --

    /// <summary>Synchronous create a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to create the persistent subscription on</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="credentials">The credentials to be used for this operation.</param>
    public static void CreatePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        connection.CreatePersistentSubscriptionAsync(stream, groupName, settings, credentials).ConfigureAwait(false).GetAwaiter().GetResult();
      }
      catch (InvalidOperationException ex)
      {
        if (!string.Equals(ex.Message,
                    string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionAlreadyExists, groupName, stream),
                    StringComparison.Ordinal))
        {
          throw ex;
        }
      }
    }

    #endregion

    #region -- DeletePersistentSubscription --

    /// <summary>Synchronous delete a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to delete the persistent subscription on</param>
    /// <param name="groupName">The name of the group to delete</param>
    /// <param name="credentials">User credentials to use for the operation</param>
    public static void DeletePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, UserCredentials credentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        connection.DeletePersistentSubscriptionAsync(stream, groupName, credentials).ConfigureAwait(false).GetAwaiter().GetResult();
      }
      catch (InvalidOperationException ex)
      {
        if (!string.Equals(ex.Message,
                           string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, groupName, stream),
                           StringComparison.Ordinal))
        {
          throw ex;
        }
      }
    }

    #endregion

    #region -- UpdatePersistentSubscription --

    /// <summary>Synchronous update a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to create the persistent subscription on</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="credentials">The credentials to be used for this operation.</param>
    public static void UpdatePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        connection.UpdatePersistentSubscriptionAsync(stream, groupName, settings, credentials).ConfigureAwait(false).GetAwaiter().GetResult();
      }
      catch (InvalidOperationException ex)
      {
        if (string.Equals(ex.Message,
                          string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, groupName, stream),
                          StringComparison.Ordinal))
        {
          CreatePersistentSubscription(connection, stream, groupName, settings, credentials);
        }
      }
    }

    #endregion

    #region -- ConnectToPersistentSubscription --

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
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
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
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
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    #endregion

    #region -- ConnectToPersistentSubscriptionAsync --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription</returns>
    public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- ReadFirstEventAsync --

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static Task<EventReadResult> ReadFirstEventAsync(this IEventStoreConnectionBase connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      return connection.ReadEventAsync(stream, StreamPosition.Start, resolveLinkTos, userCredentials);
    }

    #endregion

    #region -- ReadLastEventAsync --

    /// <summary>Asynchronously reads the last event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static async Task<EventReadResult> ReadLastEventAsync(this IEventStoreConnectionBase connection,
      string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      var slice = await connection.ReadStreamEventsBackwardAsync(stream, StreamPosition.End, 1, resolveLinkTos, userCredentials)
                                  .ConfigureAwait(false);
      var readStatus = EventReadStatus.Success;
      var sliceEvents = slice.Events;
      switch (slice.Status)
      {
        case SliceReadStatus.StreamNotFound:
          readStatus = EventReadStatus.NoStream;
          break;
        case SliceReadStatus.StreamDeleted:
          readStatus = EventReadStatus.StreamDeleted;
          break;
        case SliceReadStatus.Success:
        default:
          if (sliceEvents.Length == 0) { readStatus = EventReadStatus.NotFound; }
          break;
      }
      if (EventReadStatus.Success == readStatus)
      {
        var lastEvent = sliceEvents[0];
        return EventReadResult.Create(readStatus, slice.Stream, lastEvent.OriginalEventNumber, resolvedEvent: lastEvent);
      }
      else
      {
        return EventReadResult.Create(readStatus, slice.Stream, -1, resolvedEvent: null);
      }
    }

    #endregion

    #region -- SubscribeToStream --

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return connection.SubscribeToStreamAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return connection.SubscribeToStreamAsync(stream, settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection,
      string stream, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToStreamAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToStream(this IEventStoreConnectionBase connection,
      string stream, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToStreamAsync(stream, settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    #endregion

    #region -- SubscribeToStreamStart --

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
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamStart(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToStreamFrom(stream, StreamPosition.Start, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
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
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamStart(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToStreamFrom(stream, StreamPosition.Start, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamEndAsync --

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
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static async Task<EventStoreStreamCatchUpSubscription> SubscribeToStreamEndAsync(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == settings) { throw new ArgumentNullException(nameof(settings)); }

      long lastCheckpoint = StreamPosition.Start;
      var readResult = await ReadLastEventAsync(connection, stream, settings.ResolveLinkTos, userCredentials);
      if (EventReadStatus.Success == readResult.Status)
      {
        lastCheckpoint = readResult.EventNumber;
      }

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
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
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="settings">The <see cref="CatchUpSubscriptionSettings"/> for the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static async Task<EventStoreStreamCatchUpSubscription> SubscribeToStreamEndAsync(this IEventStoreConnectionBase connection,
      string stream, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == settings) { throw new ArgumentNullException(nameof(settings)); }

      long lastCheckpoint = StreamPosition.Start;
      var readResult = await ReadLastEventAsync(connection, stream, settings.ResolveLinkTos, userCredentials);
      if (EventReadStatus.Success == readResult.Status)
      {
        lastCheckpoint = readResult.EventNumber;
      }

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamAsync --

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToStreamAsync(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToStreamAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a single event stream. New events
    /// written to the stream while the subscription is active will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToStreamAsync(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToStreamAsync(stream, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToStreamFrom --

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
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
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
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    //[Obsolete("This method will be obsoleted in the next major version please switch to the overload with a settings object")]
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(this IEventStoreConnectionBase connection,
      string stream, long? lastCheckpoint, bool resolveLinkTos,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
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
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
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
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(this IEventStoreConnectionBase connection,
      string stream, long? lastCheckpoint, bool resolveLinkTos,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);

      return connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToAll --

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return connection.SubscribeToAllAsync(settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, bool verboseLogging = false)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos, VerboseLogging = verboseLogging };
      return connection.SubscribeToAllAsync(settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToAllAsync(settings, eventAppeared, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>Subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="settings">The <see cref="SubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreSubscription SubscribeToAll(this IEventStoreConnection connection, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      return connection.SubscribeToAllAsync(settings, eventAppearedAsync, subscriptionDropped, userCredentials)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    #endregion

    #region -- SubscribeToAllAsync --

    /// <summary>Asynchronously subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToAllAsync(this IEventStoreConnection connection, bool resolveLinkTos,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToAllAsync(settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to all events in the Event Store. New
    /// events written to the stream while the subscription is active
    /// will be pushed to the client.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnection"/> responsible for raising the event.</param>
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when a new event is received over the subscription</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static Task<EventStoreSubscription> SubscribeToAllAsync(this IEventStoreConnection connection, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var settings = new SubscriptionSettings { ResolveLinkTos = resolveLinkTos };
      return connection.SubscribeToAllAsync(settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- SubscribeToAllFrom --

    /// <summary>Subscribes to all events. Existing events from lastCheckpoint
    /// onwards are read from the Event Store and presented to the user of
    /// <see cref="EventStoreCatchUpSubscription"/> as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
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
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppeared">An action invoked when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    //[Obsolete("This overload will be removed in the next major release please use the overload with a settings object")]
    public static EventStoreAllCatchUpSubscription SubscribeToAllFrom(this IEventStoreConnection connection,
      Position? lastCheckpoint, bool resolveLinkTos,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
      return connection.SubscribeToAllFrom(lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to all events. Existing events from lastCheckpoint
    /// onwards are read from the Event Store and presented to the user of
    /// <see cref="EventStoreCatchUpSubscription"/> as if they had been pushed.
    ///
    /// Once the end of the stream is read the subscription is
    /// transparently (to the user) switched to push new events as
    /// they are written.
    ///
    /// The action liveProcessingStarted is called when the
    /// <see cref="EventStoreCatchUpSubscription"/> switches from the reading
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
    /// <param name="resolveLinkTos">Whether to resolve Link events automatically</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event is received over the subscription</param>
    /// <param name="liveProcessingStarted">An action invoked when the subscription switches to newly-pushed events</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <param name="readBatchSize">The batch size to use during the read phase</param>
    /// <param name="subscriptionName">The name of subscription</param>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>An <see cref="EventStoreSubscription"/> representing the subscription</returns>
    public static EventStoreAllCatchUpSubscription SubscribeToAllFrom(this IEventStoreConnection connection,
      Position? lastCheckpoint, bool resolveLinkTos,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int readBatchSize = 500, string subscriptionName = "", bool verboseLogging = false)
    {
      var settings = CatchUpSubscriptionSettings.Create(readBatchSize, resolveLinkTos, subscriptionName, verboseLogging);
      return connection.SubscribeToAllFrom(lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    #endregion
  }
}
