using System;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI
{
  public static class IEventStoreConnectionExtensions
  {
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
        if (!string.Equals(ex.Message, $"Subscription group {groupName} on stream {stream} already exists", StringComparison.Ordinal))
        {
          throw;
        }
      }
    }
  }
}
