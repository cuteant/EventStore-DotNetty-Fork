using System;
using System.Globalization;
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
          throw;
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
          throw;
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
  }
}
