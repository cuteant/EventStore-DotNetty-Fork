using System;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Embedded
{
    internal interface IEmbeddedSubscription
    {
        void DropSubscription(EventStore.Core.Services.SubscriptionDropReason reason, Exception ex = null);
        Task EventAppeared((EventStore.Core.Data.ResolvedEvent resolvedEvent, int? retryCount) resolvedEventWrapper);
        void ConfirmSubscription(long lastCommitPosition, long? lastEventNumber);
        void Unsubscribe();
        void Start(Guid correlationId);
    }
}