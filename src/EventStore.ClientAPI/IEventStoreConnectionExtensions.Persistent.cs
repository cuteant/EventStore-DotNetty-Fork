using System;
using System.Globalization;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  partial class IEventStoreConnectionExtensions
  {
    #region -- CreatePersistentSubscription --

    /// <summary>Synchronous create a persistent subscription group on a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The name of the stream to create the persistent subscription on</param>
    /// <param name="groupName">The name of the group to create</param>
    /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="userCredentials">The credentials to be used for this operation.</param>
    public static void CreatePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        AsyncContext.Run(
          async (conn, streamId, group, settings, credentials)
            => await conn.CreatePersistentSubscriptionAsync(streamId, group, settings, credentials).ConfigureAwait(false),
          connection, stream, groupName, subscriptionSettings, userCredentials);
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
    /// <param name="userCredentials">User credentials to use for the operation</param>
    public static void DeletePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        AsyncContext.Run(
          async (conn, streamId, group, credentials)
            => await conn.DeletePersistentSubscriptionAsync(streamId, group, credentials).ConfigureAwait(false),
          connection, stream, groupName, userCredentials);
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
    /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription</param>
    /// <param name="userCredentials">The credentials to be used for this operation.</param>
    public static void UpdatePersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }

      try
      {
        AsyncContext.Run(
          async (conn, streamId, group, settings, credentials)
            => await conn.UpdatePersistentSubscriptionAsync(streamId, group, settings, credentials).ConfigureAwait(false),
          connection, stream, groupName, subscriptionSettings, userCredentials);
      }
      catch (InvalidOperationException ex)
      {
        if (string.Equals(ex.Message,
                          string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, groupName, stream),
                          StringComparison.Ordinal))
        {
          CreatePersistentSubscription(connection, stream, groupName, subscriptionSettings, userCredentials);
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
    /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);

      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
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
    /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      ConnectToPersistentSubscriptionSettings subscriptionSettings,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="groupName">The subscription group to connect to</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
    public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      ConnectToPersistentSubscriptionSettings subscriptionSettings,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return AsyncContext.Run(
        async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
          => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
        connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
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
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
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
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
      string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion


    #region -- PersistentSubscribeAsync(NonGeneric) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
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
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreConnectionBase2 connection,
      string stream, string subscriptionId,
      Action<EventStorePersistentSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync(stream, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
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
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreConnectionBase2 connection,
      string stream, string subscriptionId,
      Func<EventStorePersistentSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync(stream, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- PersistentSubscribeAsync(NonGeneric-Topic) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="topic">The topic</param>
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
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreConnectionBase2 connection,
      string stream, string topic, string subscriptionId,
      Action<EventStorePersistentSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync(CombineStreamId(stream, topic), subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
    /// <param name="topic">The topic</param>
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
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreConnectionBase2 connection,
      string stream, string topic, string subscriptionId,
      Func<EventStorePersistentSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync(CombineStreamId(stream, topic), subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
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
    public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreConnectionBase2 connection,
      string stream, string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.PersistentSubscribeAsync(CombineStreamId(stream, topic), subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to subscribe to</param>
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
    public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreConnectionBase2 connection,
      string stream, string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscription, ResolvedEvent<object>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }
      if (string.IsNullOrEmpty(topic)) { throw new ArgumentNullException(nameof(topic)); }
      return connection.PersistentSubscribeAsync(CombineStreamId(stream, topic), subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- PersistentSubscribeAsync(Generic) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreConnectionBase2 connection, string subscriptionId,
      Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreConnectionBase2 connection, string subscriptionId,
      Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
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
    public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreConnectionBase2 connection,
      string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return connection.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
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
    public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreConnectionBase2 connection,
      string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      return connection.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion

    #region -- PersistentSubscribeAsync(Generic-Topic) --

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="eventAppeared">An action invoked when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreConnectionBase2 connection,
      string topic, string subscriptionId,
      Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync<TEvent>(topic, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
    /// <param name="topic">The topic</param>
    /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.</param>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase2"/> responsible for raising the event.</param>
    /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears</param>
    /// <param name="subscriptionDropped">An action invoked if the subscription is dropped</param>
    /// <param name="userCredentials">User credentials to use for the operation</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="bufferSize">The buffer size to use for the persistent subscription</param>
    /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
    /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
    /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
    /// must first be created with CreatePersistentSubscriptionAsync many connections
    /// can connect to the same group and they will be treated as competing consumers within the group.
    /// If one connection dies work will be balanced across the rest of the consumers in the group. If
    /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
    /// <param name="verboseLogging">Enables verbose logging on the subscription</param>
    /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
    public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreConnectionBase2 connection,
      string topic, string subscriptionId,
      Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false) where TEvent : class
    {
      if (null == connection) { throw new ArgumentNullException(nameof(connection)); }
      var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
      return connection.PersistentSubscribeAsync<TEvent>(topic, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    #endregion
  }
}
