﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
    public static partial class IEventStoreConnectionExtensions
    {
        #region -- Connect --

        /// <summary>Connects the <see cref="IEventStoreConnection"/> asynchronously to a destination.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        public static void Connect(this IEventStoreConnectionBase connection)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            AsyncContext.Run(conn => conn.ConnectAsync(), connection);
        }

        #endregion

        #region -- SetSystemSettings --

        /// <summary>Sets the global settings for the server or cluster to which the <see cref="IEventStoreConnection"/> is connected.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="sysSettings">The <see cref="SystemSettings"/> to apply.</param>
        /// <param name="userCredentials">User credentials to use for the operation.</param>
        public static void SetSystemSettings(this IEventStoreConnectionBase connection, SystemSettings sysSettings, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            AsyncContext.Run(
              async (conn, settings, credentials)
                => await conn.SetSystemSettingsAsync(settings, credentials).ConfigureAwait(false),
              connection, sysSettings, userCredentials);
        }

        #endregion

        #region -- DeleteStream --

        /// <summary>Deletes a stream from Event Store synchronously.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="DeleteResult"/> containing the results of the delete stream operation.</returns>
        public static DeleteResult DeleteStream(this IEventStoreConnectionBase connection,
          string stream, long expectedVersion, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
                      async (conn, streamId, version, credentials)
                        => await conn.DeleteStreamAsync(streamId, version, credentials).ConfigureAwait(false),
                      connection, stream, expectedVersion, userCredentials);

        }

        /// <summary>Deletes a stream from Event Store synchronously.</summary>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The name of the stream to delete.</param>
        /// <param name="expectedVersion">The expected version that the streams should have when being deleted. <see cref="ExpectedVersion"/></param>
        /// <param name="hardDelete">Indicator for tombstoning vs soft-deleting the stream. Tombstoned streams can never be recreated.
        /// Soft-deleted streams can be written to again, but the EventNumber sequence will not start from 0.</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="DeleteResult"/> containing the results of the delete stream operation.</returns>
        public static DeleteResult DeleteStream(this IEventStoreConnectionBase connection,
          string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
                      async (conn, streamId, version, hardDel, credentials)
                        => await conn.DeleteStreamAsync(streamId, version, hardDel, credentials).ConfigureAwait(false),
                      connection, stream, expectedVersion, hardDelete, userCredentials);
        }

        #endregion

        #region -- StartTransaction --

        /// <summary>Starts a transaction in Event Store on a given stream.</summary>
        /// <remarks>A <see cref="EventStoreTransaction"/> allows the calling of multiple writes with multiple
        /// round trips over long periods of time between the caller and Event Store. This method
        /// is only available through the TCP interface and no equivalent exists for the RESTful interface.</remarks>
        /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
        /// <param name="stream">The stream to start a transaction on.</param>
        /// <param name="expectedVersion">The expected version of the stream at the time of starting the transaction</param>
        /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
        /// <returns>A <see cref="EventStoreTransaction"/> representing a multi-request transaction.</returns>
        public static EventStoreTransaction StartTransaction(this IEventStoreConnectionBase connection,
          string stream, long expectedVersion, UserCredentials userCredentials = null)
        {
            if (connection is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }
            return AsyncContext.Run(
                      async (conn, streamId, version, credentials)
                        => await conn.StartTransactionAsync(streamId, version, credentials).ConfigureAwait(false),
                      connection, stream, expectedVersion, userCredentials);
        }

        #endregion

        #region ** DoWriteAsync **

        private static async Task<WriteResult> DoWriteAsync(IEventStoreConnectionBase connection, string stream, long expectedVersion,
          EventData[] eventDatas, int batchSize, UserCredentials userCredentials)
        {
            var count = eventDatas.Length;
            if (count <= batchSize)
            {
                return await connection.AppendToStreamAsync(stream, expectedVersion, eventDatas, userCredentials).ConfigureAwait(false);
            }

            using (var trans = await connection.StartTransactionAsync(stream, expectedVersion, userCredentials))
            {
                var page = 0;
                while (page < count)
                {
                    await trans.WriteAsync(eventDatas.Skip(page).Take(batchSize)).ConfigureAwait(false);
                    page += batchSize;
                }

                return await trans.CommitAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region ** class HandlerAdder **

        private sealed class HandlerAdder : IConsumerRegistration
        {
            private readonly IHandlerRegistration _handlerRegistration;

            public HandlerAdder(IHandlerRegistration handlerRegistration)
            {
                _handlerRegistration = handlerRegistration;
            }

            public IConsumerRegistration Add<T>(Func<T, Task> eventAppearedAsync)
            {
                _handlerRegistration.Add<T>(iResolvedEvent => eventAppearedAsync(iResolvedEvent.OriginalEvent.FullEvent.Value));
                return this;
            }

            public IConsumerRegistration Add<T>(Action<T> eventAppeared)
            {
                _handlerRegistration.Add<T>(iResolvedEvent => eventAppeared(iResolvedEvent.OriginalEvent.FullEvent.Value));
                return this;
            }
        }

        #endregion
    }
}
