﻿using EventStore.Core.Services;
using EventStore.Projections.Core.Messages;
using Newtonsoft.Json.Linq;

namespace EventStore.Projections.Core.Services.Processing
{
    public class ByPositionStreamStatePartitionSelector : StatePartitionSelector
    {
        public override string GetStatePartition(EventReaderSubscriptionMessage.CommittedEventReceived @event)
        {
            if (@event.Data.ResolvedLinkTo && @event.Data.PositionMetadata is object)
            {
                var extra = @event.Data.PositionMetadata.ParseCheckpointExtraJson();
                JToken v;
                if (extra is object && extra.TryGetValue("$o", out v))
                {
                    //TODO: handle exceptions properly
                    var originalStream = (string) ((JValue) v).Value;
                    return originalStream;
                }
            }

            var eventStreamId = @event.Data.PositionStreamId;
            return SystemStreams.IsMetastream(eventStreamId)
                ? eventStreamId.Substring("$$".Length)
                : eventStreamId;
        }

        public override bool EventReaderBasePartitionDeletedIsSupported()
        {
            return true;
        }
    }
}
