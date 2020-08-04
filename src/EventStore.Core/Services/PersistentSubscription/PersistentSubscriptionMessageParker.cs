using System;
using System.Text;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Helpers;
using EventStore.Core.Messages;
using EventStore.Core.Services.UserManagement;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.PersistentSubscription
{
    public class PersistentSubscriptionMessageParker : IPersistentSubscriptionMessageParker
    {
        private readonly IODispatcher _ioDispatcher;
        private readonly string _parkedStreamId;
        private static readonly ILogger Log = TraceLogger.GetLogger<PersistentSubscriptionMessageParker>();

        public PersistentSubscriptionMessageParker(string subscriptionId, IODispatcher ioDispatcher)
        {
            _parkedStreamId = "$persistentsubscription-" + subscriptionId + "-parked";
            _ioDispatcher = ioDispatcher;
        }

        private Event CreateStreamMetadataEvent(long? tb)
        {
            var eventId = Guid.NewGuid();
            var acl = new StreamAcl(
                readRole: SystemRoles.Admins, writeRole: SystemRoles.Admins,
                deleteRole: SystemRoles.Admins, metaReadRole: SystemRoles.Admins,
                metaWriteRole: SystemRoles.Admins);
            var metadata = new StreamMetadata(cacheControl: null,
                                              truncateBefore: tb,
                                              acl: acl);
            var dataBytes = metadata.ToJsonBytes();
            return new Event(eventId, SystemEventTypes.StreamMetadata, isJson: true, data: dataBytes, metadata: null);
        }

        private void WriteStateCompleted((Action<ResolvedEvent, OperationResult> completed, ResolvedEvent ev) pair, ClientMessage.WriteEventsCompleted msg)
        {
            pair.completed?.Invoke(pair.ev, msg.Result);
        }

        public void BeginParkMessage(in ResolvedEvent ev, string reason, Action<ResolvedEvent, OperationResult> completed)
        {
            var metadata = new ParkedMessageMetadata { Added = DateTime.Now, Reason = reason, SubscriptionEventNumber = ev.OriginalEventNumber };

            string data = GetLinkToFor(ev);

            var parkedEvent = new Event(Guid.NewGuid(), SystemEventTypes.LinkTo, false, data, metadata.ToJson());

            var pair = (completed, ev);
            _ioDispatcher.WriteEvent(_parkedStreamId, ExpectedVersion.Any, parkedEvent, SystemAccount.Principal, x => WriteStateCompleted(pair, x));
        }

        private string GetLinkToFor(in ResolvedEvent ev)
        {
            if (ev.Event is null) // Unresolved link so just use the bad/deleted link data.
            {
                return Encoding.UTF8.GetString(ev.Link.Data);
            }

            return $"{ev.Event.EventNumber}@{ev.Event.EventStreamId}";
        }


        public void BeginDelete(Action<IPersistentSubscriptionMessageParker> completed)
        {
            _ioDispatcher.DeleteStream(_parkedStreamId, ExpectedVersion.Any, false, SystemAccount.Principal,
                x => completed(this));
        }

        public void BeginReadEndSequence(Action<long?> completed)
        {
            _ioDispatcher.ReadBackward(_parkedStreamId,
                long.MaxValue,
                1,
                false,
                SystemAccount.Principal, comp =>
                {
                    switch (comp.Result)
                    {
                        case ReadStreamResult.Success:
                            completed(comp.LastEventNumber);
                            break;
                        case ReadStreamResult.NoStream:
                            completed(null);
                            break;
                        default:
                            Log.AnErrorOccuredReadingTheLastEventInTheParkedMessageStream(_parkedStreamId, comp.Result);
                            break;
                    }
                });
        }

        public void BeginMarkParkedMessagesReprocessed(long sequence)
        {
            var metaStreamId = SystemStreams.MetastreamOf(_parkedStreamId);
            _ioDispatcher.WriteEvent(
                metaStreamId, ExpectedVersion.Any, CreateStreamMetadataEvent(sequence), SystemAccount.Principal, msg =>
                {
                    switch (msg.Result)
                    {
                        case OperationResult.Success:
                      //nothing
                      break;
                        default:
                            Log.AnErrorOccuredTruncatingTheParkedMessageStream(_parkedStreamId, msg.Result);
                            break;
                    }
                });
        }

        class ParkedMessageMetadata
        {
            public DateTime Added { get; set; }
            public string Reason { get; set; }
            public long SubscriptionEventNumber { get; set; }
        }
    }
}
