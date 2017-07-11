using EventStore.ClientAPI.ClientOperations;

namespace EventStore.ClientAPI.Internal
{
    internal class VolatileEventStoreSubscription : EventStoreSubscription
    {
        private readonly IVolatileSubscriptionOperation _subscriptionOperation;

        internal VolatileEventStoreSubscription(IVolatileSubscriptionOperation subscriptionOperation, string streamId, long lastCommitPosition, long? lastEventNumber)
            : base(streamId, lastCommitPosition, lastEventNumber)
        {
            _subscriptionOperation = subscriptionOperation;
        }

        public override void Unsubscribe()
        {
            _subscriptionOperation.Unsubscribe();
        }

        public override long ProcessingEventNumber => _subscriptionOperation.ProcessingEventNumber;
  }
}