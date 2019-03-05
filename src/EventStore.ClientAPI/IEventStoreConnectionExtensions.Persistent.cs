using System;
using System.Globalization;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    partial class IEventStoreConnectionExtensions
    {
        #region -- CreatePersistentSubscriptionAsync --

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task CreatePersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.CreatePersistentSubscriptionAsync(stream.Combine(topic), groupName, settings, credentials);
        }

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task CreatePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return connection.CreatePersistentSubscriptionAsync(EventManager.GetStreamId<TEvent>(), groupName, settings, credentials);
        }

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task CreatePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return connection.CreatePersistentSubscriptionAsync(EventManager.GetStreamId<TEvent>(topic), groupName, settings, credentials);
        }

        #endregion

        #region -- CreatePersistentSubscriptionIfNotExists --

        /// <summary>Create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void CreatePersistentSubscriptionIfNotExists(this IEventStoreConnectionBase connection,
            string stream, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

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

        /// <summary>Create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void CreatePersistentSubscriptionIfNotExists(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            CreatePersistentSubscriptionIfNotExists(connection, stream.Combine(topic), groupName, subscriptionSettings, userCredentials);
        }

        /// <summary>Create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void CreatePersistentSubscriptionIfNotExists<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            CreatePersistentSubscriptionIfNotExists(connection, EventManager.GetStreamId<TEvent>(), groupName, subscriptionSettings, userCredentials);
        }

        /// <summary>Create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void CreatePersistentSubscriptionIfNotExists<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            CreatePersistentSubscriptionIfNotExists(connection, EventManager.GetStreamId<TEvent>(topic), groupName, subscriptionSettings, userCredentials);
        }

        #endregion

        #region -- CreatePersistentSubscriptionIfNotExistsAsync -- 

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static async Task CreatePersistentSubscriptionIfNotExistsAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            try
            {
                await connection.CreatePersistentSubscriptionAsync(stream, groupName, settings, userCredentials).ConfigureAwait(false);
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

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task CreatePersistentSubscriptionIfNotExistsAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return CreatePersistentSubscriptionIfNotExistsAsync(connection, stream.Combine(topic), groupName, settings, credentials);
        }

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task CreatePersistentSubscriptionIfNotExistsAsync<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            return CreatePersistentSubscriptionIfNotExistsAsync(connection, EventManager.GetStreamId<TEvent>(), groupName, settings, credentials);
        }

        /// <summary>Asynchronously create a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task CreatePersistentSubscriptionIfNotExistsAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return CreatePersistentSubscriptionIfNotExistsAsync(connection, EventManager.GetStreamId<TEvent>(topic), groupName, settings, credentials);
        }

        #endregion

        #region -- UpdatePersistentSubscriptionAsync --

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task UpdatePersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return connection.UpdatePersistentSubscriptionAsync(stream.Combine(topic), groupName, settings, credentials);
        }

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task UpdatePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return connection.UpdatePersistentSubscriptionAsync(EventManager.GetStreamId<TEvent>(), groupName, settings, credentials);
        }

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task UpdatePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return connection.UpdatePersistentSubscriptionAsync(EventManager.GetStreamId<TEvent>(topic), groupName, settings, credentials);
        }

        #endregion

        #region -- UpdatePersistentSubscription --

        /// <summary>Update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void UpdateOrCreatePersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

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
                    CreatePersistentSubscriptionIfNotExists(connection, stream, groupName, subscriptionSettings, userCredentials);
                    return;
                }
                throw ex;
            }
        }

        /// <summary>Update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void UpdateOrCreatePersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            UpdateOrCreatePersistentSubscription(connection, stream.Combine(topic), groupName, subscriptionSettings, userCredentials);
        }

        /// <summary>Update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void UpdateOrCreatePersistentSubscription<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            UpdateOrCreatePersistentSubscription(connection, EventManager.GetStreamId<TEvent>(), groupName, subscriptionSettings, userCredentials);
        }

        /// <summary>Update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="subscriptionSettings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="userCredentials">The credentials to be used for this operation.</param>
        public static void UpdateOrCreatePersistentSubscription<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, PersistentSubscriptionSettings subscriptionSettings, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            UpdateOrCreatePersistentSubscription(connection, EventManager.GetStreamId<TEvent>(topic), groupName, subscriptionSettings, userCredentials);
        }

        #endregion

        #region -- UpdateOrCreatePersistentSubscriptionAsync --

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static async Task UpdateOrCreatePersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            try
            {
                await connection.UpdatePersistentSubscriptionAsync(stream, groupName, settings, credentials).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message,
                                  string.Format(CultureInfo.InvariantCulture, Consts.PersistentSubscriptionDoesNotExist, groupName, stream),
                                  StringComparison.Ordinal))
                {
                    await CreatePersistentSubscriptionIfNotExistsAsync(connection, stream, groupName, settings, credentials).ConfigureAwait(false);
                    return;
                }
                throw ex;
            }
        }

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to create the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task UpdateOrCreatePersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return UpdateOrCreatePersistentSubscriptionAsync(connection, stream.Combine(topic), groupName, settings, credentials);
        }

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task UpdateOrCreatePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return UpdateOrCreatePersistentSubscriptionAsync(connection, EventManager.GetStreamId<TEvent>(), groupName, settings, credentials);
        }

        /// <summary>Asynchronously update a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to create.</param>
        /// <param name="settings">The <see cref="PersistentSubscriptionSettings"></see> for the subscription.</param>
        /// <param name="credentials">The credentials to be used for this operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task UpdateOrCreatePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return UpdateOrCreatePersistentSubscriptionAsync(connection, EventManager.GetStreamId<TEvent>(topic), groupName, settings, credentials);
        }

        #endregion

        #region -- DeletePersistentSubscriptionAsync --

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task DeletePersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.DeletePersistentSubscriptionAsync(stream.Combine(topic), groupName, userCredentials);
        }

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task DeletePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return connection.DeletePersistentSubscriptionAsync(EventManager.GetStreamId<TEvent>(), groupName, userCredentials);
        }

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task DeletePersistentSubscriptionAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return connection.DeletePersistentSubscriptionAsync(EventManager.GetStreamId<TEvent>(topic), groupName, userCredentials);
        }

        #endregion

        #region -- DeletePersistentSubscriptionIfExists --

        /// <summary>Delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        public static void DeletePersistentSubscriptionIfExists(this IEventStoreConnectionBase connection,
            string stream, string groupName, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

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

        /// <summary>Delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        public static void DeletePersistentSubscriptionIfExists(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            DeletePersistentSubscriptionIfExists(connection, stream.Combine(topic), groupName, userCredentials);
        }

        /// <summary>Delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        public static void DeletePersistentSubscriptionIfExists<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, UserCredentials userCredentials = null)
        {
            DeletePersistentSubscriptionIfExists(connection, EventManager.GetStreamId<TEvent>(), groupName, userCredentials);
        }

        /// <summary>Delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        public static void DeletePersistentSubscriptionIfExists<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, UserCredentials userCredentials = null)
        {
            DeletePersistentSubscriptionIfExists(connection, EventManager.GetStreamId<TEvent>(topic), groupName, userCredentials);
        }

        #endregion

        #region -- DeletePersistentSubscriptionIfExistsAsync --

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete the persistent subscription on.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static async Task DeletePersistentSubscriptionIfExistsAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName, UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            try
            {
                await connection.DeletePersistentSubscriptionAsync(stream, groupName, userCredentials).ConfigureAwait(false);
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

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete the persistent subscription on.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task DeletePersistentSubscriptionIfExistsAsync(this IEventStoreConnectionBase connection,
            string stream, string topic, string groupName, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return DeletePersistentSubscriptionIfExistsAsync(connection, stream.Combine(topic), groupName, userCredentials);
        }

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task DeletePersistentSubscriptionIfExistsAsync<TEvent>(this IEventStoreConnectionBase connection,
            string groupName, UserCredentials userCredentials = null)
        {
            return DeletePersistentSubscriptionIfExistsAsync(connection, EventManager.GetStreamId<TEvent>(), groupName, userCredentials);
        }

        /// <summary>Asynchronously delete a persistent subscription group on a stream.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="groupName">The name of the group to delete</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <returns>A <see cref="Task"/> that can be waited upon.</returns>
        public static Task DeletePersistentSubscriptionIfExistsAsync<TEvent>(this IEventStoreConnectionBase connection,
            string topic, string groupName, UserCredentials userCredentials = null)
        {
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return DeletePersistentSubscriptionIfExistsAsync(connection, EventManager.GetStreamId<TEvent>(topic), groupName, userCredentials);
        }

        #endregion


        #region -- ConnectToPersistentSubscription --

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
        public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);

            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
        public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
        public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            return connection.ConnectToPersistentSubscription(stream, groupName, (s, e, r) => eventAppeared(s, e), subscriptionDropped, userCredentials, bufferSize, autoAck, verboseLogging);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
        public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            return connection.ConnectToPersistentSubscription(stream, groupName, (s, e, r) => eventAppearedAsync(s, e), subscriptionDropped, userCredentials, bufferSize, autoAck, verboseLogging);
        }


        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
        public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscriptionBase"/> representing the subscription.</returns>
        public static EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.ConnectToPersistentSubscriptionAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                connection, Tuple.Create(stream, groupName), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
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
            return connection.ConnectToPersistentSubscription(stream, groupName, subscriptionSettings, (s, e, r) => eventAppeared(s, e), subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
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
            return connection.ConnectToPersistentSubscription(stream, groupName, subscriptionSettings, (s, e, r) => eventAppearedAsync(s, e), subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- ConnectToPersistentSubscriptionAsync --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent, int?> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, (s, e, r) => eventAppeared(s, e), subscriptionDropped, userCredentials, bufferSize, autoAck, verboseLogging);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, (s, e, r) => eventAppearedAsync(s, e), subscriptionDropped, userCredentials, bufferSize, autoAck, verboseLogging);
        }


        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            ConnectToPersistentSubscriptionSettings settings,
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, (s, e, r) => eventAppeared(s, e), subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="groupName">The subscription group to connect to.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscriptionBase&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(this IEventStoreConnectionBase connection,
            string stream, string groupName,
            ConnectToPersistentSubscriptionSettings settings,
            Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            return connection.ConnectToPersistentSubscriptionAsync(stream, groupName, settings, (s, e, r) => eventAppearedAsync(s, e), subscriptionDropped, userCredentials);
        }

        #endregion


        #region -- PersistentSubscribe(NonGeneric) --

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string subscriptionId,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream, subscriptionId), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string subscriptionId,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream, subscriptionId), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }
        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream, subscriptionId), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream, subscriptionId), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribe(NonGeneric-Topic) --

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream.Combine(topic), subscriptionId), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream.Combine(topic), subscriptionId), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream.Combine(topic), subscriptionId), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription PersistentSubscribe(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }

            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(stream.Combine(topic), subscriptionId), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribe(Generic) --

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus, string subscriptionId,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, subId, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(null, subId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, subscriptionId, subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus, string subscriptionId,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }

            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, subId, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(null, subId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, subscriptionId, subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus,
            string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return AsyncContext.Run(
                async (conn, subId, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(null, subId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, subscriptionId, subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus,
            string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return AsyncContext.Run(
                async (conn, subId, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(null, subId, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, subscriptionId, subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribe(Generic-Topic) --

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus,
            string topic, string subscriptionId,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(topic, subscriptionId), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus,
            string topic, string subscriptionId,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var subscriptionSettings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(topic, subscriptionId), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus,
            string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(topic, subscriptionId), subscriptionSettings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionSettings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="EventStorePersistentSubscription"/> representing the subscription.</returns>
        public static EventStorePersistentSubscription<TEvent> PersistentSubscribe<TEvent>(this IEventStoreBus bus,
            string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings subscriptionSettings,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return AsyncContext.Run(
                async (conn, streamWrapper, settings, eAppeared, subDropped, credentials)
                    => await conn.PersistentSubscribeAsync<TEvent>(streamWrapper.Item1, streamWrapper.Item2, settings, eAppeared, subDropped, credentials).ConfigureAwait(false),
                bus, Tuple.Create(topic, subscriptionId), subscriptionSettings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion


        #region -- PersistentSubscribeAsync(NonGeneric) --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string subscriptionId,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string subscriptionId,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribeAsync(NonGeneric-Topic) --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
            Action<EventStorePersistentSubscription, ResolvedEvent<object>, int?> eventAppeared,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
            Func<EventStorePersistentSubscription, ResolvedEvent<object>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribeAsync(Generic) --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreBus bus, string subscriptionId,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreBus bus, string subscriptionId,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreBus bus,
            string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return bus.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreBus bus,
            string subscriptionId, ConnectToPersistentSubscriptionSettings settings,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return bus.PersistentSubscribeAsync<TEvent>(null, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribeAsync(Generic-Topic) --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppeared">An action invoked when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreBus bus,
            string topic, string subscriptionId,
            Action<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?> eventAppeared,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync<TEvent>(topic, subscriptionId, settings, eventAppeared, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="eventAppearedAsync">A Task invoked and awaited when an event appears.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription<TEvent>> PersistentSubscribeAsync<TEvent>(this IEventStoreBus bus,
            string topic, string subscriptionId,
            Func<EventStorePersistentSubscription<TEvent>, ResolvedEvent<TEvent>, int?, Task> eventAppearedAsync,
            Action<EventStorePersistentSubscription<TEvent>, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync<TEvent>(topic, subscriptionId, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
        }

        #endregion


        #region -- PersistentSubscribeAsync(Multi-Handler) --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string subscriptionId, Action<IConsumerRegistration> addHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream, subscriptionId, settings, _ => addHandlers(new HandlerAdder(_)), subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string subscriptionId,
            ConnectToPersistentSubscriptionSettings settings, Action<IConsumerRegistration> addHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            return bus.PersistentSubscribeAsync(stream, subscriptionId, settings, _ => addHandlers(new HandlerAdder(_)), subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="addEventHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string subscriptionId, Action<IHandlerRegistration> addEventHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream, subscriptionId, settings, addEventHandlers, subscriptionDropped, userCredentials);
        }

        #endregion

        #region -- PersistentSubscribeAsync(Multi-Handler-Topic) --

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId, Action<IConsumerRegistration> addHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, _ => addHandlers(new HandlerAdder(_)), subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="addHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId,
            ConnectToPersistentSubscriptionSettings settings, Action<IConsumerRegistration> addHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, _ => addHandlers(new HandlerAdder(_)), subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="addEventHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <param name="bufferSize">The buffer size to use for the persistent subscription.</param>
        /// <param name="autoAck">Whether the subscription should automatically acknowledge messages processed.
        /// If not set the receiver is required to explicitly acknowledge messages through the subscription.</param>
        /// <param name="verboseLogging">Enables verbose logging on the subscription.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with <c>CreatePersistentSubscriptionAsync</c> many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId, Action<IHandlerRegistration> addEventHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true, bool verboseLogging = false)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            var settings = new ConnectToPersistentSubscriptionSettings(bufferSize, autoAck, verboseLogging);
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, addEventHandlers, subscriptionDropped, userCredentials);
        }

        /// <summary>Asynchronously subscribes to a persistent subscription(competing consumer) on event store.</summary>
        /// <param name="subscriptionId">A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.</param>
        /// <param name="bus">The <see cref="IEventStoreBus"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to subscribe to.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="settings">The <see cref="ConnectToPersistentSubscriptionSettings"/> for the subscription.</param>
        /// <param name="addEventHandlers">A function to add handlers to the consumer.</param>
        /// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        /// <remarks>This will connect you to a persistent subscription group for a stream. The subscription group
        /// must first be created with CreatePersistentSubscriptionAsync many connections
        /// can connect to the same group and they will be treated as competing consumers within the group.
        /// If one connection dies work will be balanced across the rest of the consumers in the group. If
        /// you attempt to connect to a group that does not exist you will be given an exception.</remarks>
        /// <returns>A <see cref="Task&lt;EventStorePersistentSubscription2&gt;"/> representing the subscription.</returns>
        public static Task<EventStorePersistentSubscription2> PersistentSubscribeAsync(this IEventStoreBus bus,
            string stream, string topic, string subscriptionId,
            ConnectToPersistentSubscriptionSettings settings, Action<IHandlerRegistration> addEventHandlers,
            Action<EventStorePersistentSubscription2, SubscriptionDropReason, Exception> subscriptionDropped = null,
            UserCredentials userCredentials = null)
        {
            if (null == bus) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bus); }
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }
            if (string.IsNullOrEmpty(topic)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.topic); }
            return bus.PersistentSubscribeAsync(stream.Combine(topic), subscriptionId, settings, addEventHandlers, subscriptionDropped, userCredentials);
        }

        #endregion
    }
}
