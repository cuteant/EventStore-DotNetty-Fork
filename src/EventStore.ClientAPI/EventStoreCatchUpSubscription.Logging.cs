using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI
{
    /// <summary>Base class representing catch-up subscriptions.</summary>
    partial class EventStoreCatchUpSubscription<TSubscription, TResolvedEvent>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCatchupSubscriptionStarting()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: starting...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogWaitingOnSubscriptionToStop()
        {
            Log.LogDebug("Waiting on subscription {0} to stop", SubscriptionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCatchupSubscriptionRequestingStop()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: requesting stop...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
            Log.LogDebug("Catch-up Subscription {0} to {1}: unhooking from connection.Connected.", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogCatchupSubscriptionRecoveringAfterReconnection()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: recovering after reconnection.", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
            Log.LogDebug("Catch-up Subscription {0} to {1}: unhooking from connection.Connected.", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogWaitingOnSubscriptionRunning()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: running...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogWaitingOnSubscriptionPullingEvents()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: pulling events...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogWaitingOnSubscriptionPullingEventsIfLeft()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: pulling events (if left)...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogProcessingLiveEvents()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: processing live events...", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogHookingToConnectionConnected()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: hooking to connection.Connected", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogEventAppeared(in TResolvedEvent e)
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: event appeared ({2}, {3}, {4} @ {5}).",
                      SubscriptionName,
                      IsSubscribedToAll ? "<all>" : StreamId,
                      e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEventType, e.OriginalPosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogExceptionOccurredInSubscription(Exception exc)
        {
            Log.LogDebug("Catch-up Subscription {0} to {1} Exception occurred in subscription {1}", SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, exc);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogDroppingSubscription(SubscriptionDropReason reason, Exception error)
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: dropping subscription, reason: {2} {3}.",
                      SubscriptionName,
                      IsSubscribedToAll ? "<all>" : StreamId,
                      reason, error == null ? string.Empty : error.ToString());
        }
    }

    partial class EventStoreAllCatchUpSubscription
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogFinishedReadingEvents()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: finished reading events, nextReadPosition = {2}.",
                SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, _nextReadPosition);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogProcessEvent(bool processed, in ResolvedEvent e)
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                        SubscriptionName,
                        IsSubscribedToAll ? "<all>" : StreamId,
                        processed ? "processed" : "skipping",
                        e.OriginalEvent.EventStreamId, e.OriginalEvent.EventNumber, e.OriginalEvent.EventType, e.OriginalPosition);
        }
    }

    partial class EventStoreStreamCatchUpSubscriptionBase<TSubscription, TStreamEventsSlice, TResolvedEvent>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogFinishedReadingEvents()
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: finished reading events, nextReadEventNumber = {2}.",
                SubscriptionName, IsSubscribedToAll ? "<all>" : StreamId, _nextReadEventNumber);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogProcessEvent(bool processed, in TResolvedEvent e)
        {
            Log.LogDebug("Catch-up Subscription {0} to {1}: {2} event ({3}, {4}, {5} @ {6}).",
                        SubscriptionName,
                        IsSubscribedToAll ? "<all>" : StreamId, processed ? "processed" : "skipping",
                        e.OriginalStreamId, e.OriginalEventNumber, e.OriginalEventType, e.OriginalEventNumber);
        }
    }
}
