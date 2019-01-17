using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI.ClientOperations;
using EventStore.ClientAPI.Transport.Tcp;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Internal
{
    internal class SubscriptionItem
    {
        public readonly ISubscriptionOperation Operation;
        public readonly int MaxRetries;
        public readonly TimeSpan Timeout;
        public readonly DateTime CreatedTime;

        public Guid ConnectionId;
        public Guid CorrelationId;
        public bool IsSubscribed;
        public int RetryCount;
        public DateTime LastUpdated;

        public SubscriptionItem(ISubscriptionOperation operation, int maxRetries, TimeSpan timeout)
        {
            if (null == operation) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.operation); }

            Operation = operation;
            MaxRetries = maxRetries;
            Timeout = timeout;
            CreatedTime = DateTime.UtcNow;

            CorrelationId = Guid.NewGuid();
            RetryCount = 0;
            LastUpdated = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return string.Format("Subscription {0} ({1:D}): {2}, is subscribed: {3}, retry count: {4}, "
                                 + "created: {5:HH:mm:ss.fff}, last updated: {6:HH:mm:ss.fff}",
                                 Operation.GetType().Name, CorrelationId, Operation, IsSubscribed, RetryCount, CreatedTime, LastUpdated);
        }
    }

    internal partial class SubscriptionsManager
    {
        private static readonly ILogger s_logger = TraceLogger.GetLogger<SubscriptionsManager>();
        private readonly string _connectionName;
        private readonly ConnectionSettings _settings;
        private readonly bool _verboseLogging;
        private readonly Dictionary<Guid, SubscriptionItem> _activeSubscriptions = new Dictionary<Guid, SubscriptionItem>();
        private readonly Queue<SubscriptionItem> _waitingSubscriptions = new Queue<SubscriptionItem>();
        private readonly List<SubscriptionItem> _retryPendingSubscriptions = new List<SubscriptionItem>();

        public SubscriptionsManager(string connectionName, ConnectionSettings settings)
        {
            if (null == connectionName) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connectionName); }
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }
            _connectionName = connectionName;
            _settings = settings;
            _verboseLogging = _settings.VerboseLogging && s_logger.IsDebugLevelEnabled();
        }

        public bool TryGetActiveSubscription(Guid correlationId, out SubscriptionItem subscription)
        {
            return _activeSubscriptions.TryGetValue(correlationId, out subscription);
        }

        public void CleanUp()
        {
            var connectionClosedException = CoreThrowHelper.GetConnectionClosedException(_connectionName);
            foreach (var subscription in _activeSubscriptions.Values
                                         .Concat(_waitingSubscriptions)
                                         .Concat(_retryPendingSubscriptions))
            {
                subscription.Operation.DropSubscription(SubscriptionDropReason.ConnectionClosed, connectionClosedException);
            }
            _activeSubscriptions.Clear();
            _waitingSubscriptions.Clear();
            _retryPendingSubscriptions.Clear();
        }

        public void PurgeSubscribedAndDroppedSubscriptions(Guid connectionId)
        {
            var subscriptionsToRemove = new List<SubscriptionItem>();
            foreach (var subscription in _activeSubscriptions.Values.Where(x => x.IsSubscribed && x.ConnectionId == connectionId))
            {
                subscription.Operation.ConnectionClosed();
                subscriptionsToRemove.Add(subscription);
            }
            foreach (var subscription in subscriptionsToRemove)
            {
                _activeSubscriptions.Remove(subscription.CorrelationId);
            }
        }

        public void CheckTimeoutsAndRetry(TcpPackageConnection connection)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            var retrySubscriptions = new List<SubscriptionItem>();
            var removeSubscriptions = new List<SubscriptionItem>();
            foreach (var subscription in _activeSubscriptions.Values)
            {
                if (subscription.IsSubscribed) continue;
                if (subscription.ConnectionId != connection.ConnectionId)
                {
                    retrySubscriptions.Add(subscription);
                }
                else if (subscription.Timeout > TimeSpan.Zero && DateTime.UtcNow - subscription.LastUpdated > _settings.OperationTimeout)
                {
                    LogSubscriptionNeverGotConfirmationFromServer(subscription);

                    if (_settings.FailOnNoServerResponse)
                    {
                        subscription.Operation.DropSubscription(SubscriptionDropReason.SubscribingError, CoreThrowHelper.GetOperationTimedOutException(_connectionName, subscription));
                        removeSubscriptions.Add(subscription);
                    }
                    else
                    {
                        retrySubscriptions.Add(subscription);
                    }
                }
            }

            foreach (var subscription in retrySubscriptions)
            {
                ScheduleSubscriptionRetry(subscription);
            }
            foreach (var subscription in removeSubscriptions)
            {
                RemoveSubscription(subscription);
            }

            if (_retryPendingSubscriptions.Count > 0)
            {
                foreach (var subscription in _retryPendingSubscriptions)
                {
                    subscription.RetryCount += 1;
                    StartSubscription(subscription, connection);
                }
                _retryPendingSubscriptions.Clear();
            }

            while (_waitingSubscriptions.Count > 0)
            {
                StartSubscription(_waitingSubscriptions.Dequeue(), connection);
            }
        }

        public bool RemoveSubscription(SubscriptionItem subscription)
        {
            var res = _activeSubscriptions.Remove(subscription.CorrelationId);
            if (_verboseLogging) LogRemoveSubscription(subscription, res);
            return res;
        }

        public void ScheduleSubscriptionRetry(SubscriptionItem subscription)
        {
            if (!RemoveSubscription(subscription))
            {
                if (_verboseLogging) LogRemoveSubscriptionFailedWhenTryingToRetry(subscription);
                return;
            }

            if (subscription.MaxRetries >= 0 && subscription.RetryCount >= subscription.MaxRetries)
            {
                if (_verboseLogging) LogRetriesLimitReachedWhenTryingToRetry(subscription);
                subscription.Operation.DropSubscription(SubscriptionDropReason.SubscribingError,
                                                        CoreThrowHelper.GetRetriesLimitReachedException(subscription));
                return;
            }

            if (_verboseLogging) LogRetryingSubscription(subscription);
            _retryPendingSubscriptions.Add(subscription);
        }

        public void EnqueueSubscription(SubscriptionItem subscriptionItem)
        {
            _waitingSubscriptions.Enqueue(subscriptionItem);
        }

        public void StartSubscription(SubscriptionItem subscription, TcpPackageConnection connection)
        {
            if (null == connection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.connection); }

            if (subscription.IsSubscribed)
            {
                if (_verboseLogging) LogStartSubscriptionRemovingDueToAlreadySubscribed(subscription);
                RemoveSubscription(subscription);
                return;
            }

            subscription.CorrelationId = Guid.NewGuid();
            subscription.ConnectionId = connection.ConnectionId;
            subscription.LastUpdated = DateTime.UtcNow;

            _activeSubscriptions.Add(subscription.CorrelationId, subscription);

            if (!subscription.Operation.Subscribe(subscription.CorrelationId, connection))
            {
                if (_verboseLogging) LogStartSubscriptionRemovingAsCouldNotSubscribe(subscription);
                RemoveSubscription(subscription);
            }
            else
            {
                if (_verboseLogging) LogStartSubscriptionSubscribing(subscription);
            }
        }

        private void LogDebug(string message, params object[] parameters)
        {
            s_logger.LogDebug("EventStoreConnection '{0}': {1}.", _connectionName, parameters.Length == 0 ? message : string.Format(message, parameters));
        }
    }
}
