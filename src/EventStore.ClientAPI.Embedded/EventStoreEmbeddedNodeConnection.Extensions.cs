using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Internal;
using EventStore.ClientAPI.SystemData;
using EventStore.Core.Authentication;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Services.UserManagement;
using Message = EventStore.Core.Messaging.Message;

namespace EventStore.ClientAPI.Embedded
{
  internal partial class EventStoreEmbeddedNodeConnection : IEventStoreConnection, IEventStoreTransactionConnection
  {
    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var source = new TaskCompletionSource<EventStoreSubscription>();

      Guid corrId = Guid.NewGuid();

      _subscriptions.StartSubscription(corrId, source, stream, GetUserCredentials(_settings, userCredentials), resolveLinkTos, eventAppearedAsync, subscriptionDropped);

      return source.Task;
    }
    public Task<EventStoreSubscription> SubscribeToAllAsync(bool resolveLinkTos,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var source = new TaskCompletionSource<EventStoreSubscription>();

      Guid corrId = Guid.NewGuid();

      _subscriptions.StartSubscription(corrId, source, string.Empty, GetUserCredentials(_settings, userCredentials), resolveLinkTos, eventAppearedAsync, subscriptionDropped);

      return source.Task;
    }


    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(settings, nameof(settings));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));
      var catchUpSubscription =
              new EventStoreStreamCatchUpSubscription(this, stream, lastCheckpoint,
                                                      userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                      subscriptionDropped, settings);
      catchUpSubscription.Start();
      return catchUpSubscription;
    }
    public EventStoreAllCatchUpSubscription SubscribeToAllFrom(Position? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));
      Ensure.NotNull(settings, nameof(settings));
      var catchUpSubscription =
              new EventStoreAllCatchUpSubscription(this, lastCheckpoint,
                                                   userCredentials, eventAppearedAsync, liveProcessingStarted,
                                                   subscriptionDropped, settings);
      catchUpSubscription.Start();
      return catchUpSubscription;
    }

    public EventStorePersistentSubscriptionBase ConnectToPersistentSubscription(string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10,
      bool autoAck = true)
    {
      Ensure.NotNullOrEmpty(groupName, nameof(groupName));
      Ensure.NotNullOrEmpty(stream, nameof(stream));
      Ensure.NotNull(eventAppearedAsync, nameof(eventAppearedAsync));

      var subscription = new EmbeddedEventStorePersistentSubscription(groupName, stream, eventAppearedAsync, subscriptionDropped,
          GetUserCredentials(_settings, userCredentials), _settings.VerboseLogging, _settings, _subscriptions, bufferSize,
          autoAck);

      subscription.Start().Wait();

      return subscription;
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null, int bufferSize = 10, bool autoAck = true)
    {
      var subscription = new EmbeddedEventStorePersistentSubscription(groupName, stream, eventAppearedAsync, subscriptionDropped,
          GetUserCredentials(_settings, userCredentials), _settings.VerboseLogging, _settings, _subscriptions, bufferSize,
          autoAck);

      return subscription.Start();
    }
  }
}
