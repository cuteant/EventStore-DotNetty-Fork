using System;
using System.Globalization;
using System.Threading.Tasks;
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
    public static void CreatePersistentSubscription(this IEventStoreConnectionBase connection, string stream, string groupName,
      PersistentSubscriptionSettings settings, UserCredentials credentials = null)
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
    public static void DeletePersistentSubscription(this IEventStoreConnectionBase connection, string stream, string groupName,
      UserCredentials credentials = null)
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
    public static void UpdatePersistentSubscription(this IEventStoreConnectionBase connection, string stream, string groupName,
      PersistentSubscriptionSettings settings, UserCredentials credentials = null)
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

    #region -- ReadFirstEventAsync --

    /// <summary>Asynchronously reads the frist event from a stream.</summary>
    /// <param name="connection">The <see cref="IEventStoreConnectionBase"/> responsible for raising the event.</param>
    /// <param name="stream">The stream to read from</param>
    /// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <returns>A <see cref="Task&lt;EventReadResult&gt;"/> containing the results of the read operation</returns>
    public static Task<EventReadResult> ReadFirstEventAsync(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
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
    public static async Task<EventReadResult> ReadLastEventAsync(this IEventStoreConnectionBase connection, string stream, bool resolveLinkTos, UserCredentials userCredentials = null)
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
  }
}
