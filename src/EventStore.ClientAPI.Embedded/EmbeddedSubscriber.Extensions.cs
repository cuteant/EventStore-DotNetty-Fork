using System;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Embedded
{
  internal partial class EmbeddedSubscriber
  {
    public void StartSubscription(Guid correlationId, TaskCompletionSource<EventStoreSubscription> source, string stream, UserCredentials userCredentials, bool resolveLinkTos, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
    {
      var subscription = new EmbeddedSubscription(
          _publisher, _connectionId, source, stream, userCredentials, _authenticationProvider,
          resolveLinkTos, eventAppearedAsync,
          subscriptionDropped);

      _subscriptions.StartSubscription(correlationId, subscription);
    }

    public void StartPersistentSubscription(Guid correlationId, TaskCompletionSource<PersistentEventStoreSubscription> source, string subscriptionId, string streamId, UserCredentials userCredentials, int bufferSize, Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync, Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, int maxRetries, TimeSpan operationTimeout)
    {
      var subscription = new EmbeddedPersistentSubscription(_publisher, _connectionId, source,
          subscriptionId, streamId, userCredentials, _authenticationProvider, bufferSize, eventAppearedAsync,
          subscriptionDropped, maxRetries, operationTimeout);

      _subscriptions.StartSubscription(correlationId, subscription);
    }
  }
}
