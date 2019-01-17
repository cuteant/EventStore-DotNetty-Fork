using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
    partial class SubscriptionsManager
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogSubscriptionNeverGotConfirmationFromServer(SubscriptionItem subscription)
        {
            var err = $"EventStoreConnection '{_connectionName}': subscription never got confirmation from server.\n UTC now: {DateTime.UtcNow:HH:mm:ss.fff}, operation: {subscription}.";
            s_logger.LogError(err);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRemoveSubscription(SubscriptionItem subscription, bool res)
        {
            LogDebug("RemoveSubscription {0}, result {1}.", subscription, res);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRemoveSubscriptionFailedWhenTryingToRetry(SubscriptionItem subscription)
        {
            LogDebug("RemoveSubscription failed when trying to retry {0}.", subscription);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRetriesLimitReachedWhenTryingToRetry(SubscriptionItem subscription)
        {
            LogDebug("RETRIES LIMIT REACHED when trying to retry {0}.", subscription);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRetryingSubscription(SubscriptionItem subscription)
        {
            LogDebug("retrying subscription {0}.", subscription);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartSubscriptionRemovingDueToAlreadySubscribed(SubscriptionItem subscription)
        {
            LogDebug("StartSubscription REMOVING due to already subscribed {0}.", subscription);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartSubscriptionRemovingAsCouldNotSubscribe(SubscriptionItem subscription)
        {
            LogDebug("StartSubscription REMOVING AS COULD NOT SUBSCRIBE {0}.", subscription);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogStartSubscriptionSubscribing(SubscriptionItem subscription)
        {
            LogDebug("StartSubscription SUBSCRIBING {0}.", subscription);
        }
    }
}
