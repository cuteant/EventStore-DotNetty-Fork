using System;
using System.Collections.Generic;

namespace EventStore.ClientAPI
{
    public interface IEventAdapter
    {
        IEventMetadata ToEventMetadata(Dictionary<string, object> context);

        IEventMetadata ToEventMetadata(byte[] metadata);

        EventData Adapt(object message, IEventMetadata eventMeta);

        object Adapt(byte[] eventData, byte[] metadata);

        object Adapt(byte[] eventData, IEventMetadata eventMeta);
    }

    public abstract class EventAdapter<TMeta> : IEventAdapter
        where TMeta : IEventMetadata, new()
    {
        EventData IEventAdapter.Adapt(object message, IEventMetadata eventMeta) => this.Adapt(message, (TMeta)eventMeta);
        object IEventAdapter.Adapt(byte[] eventData, byte[] metadata) => Adapt(eventData, (TMeta)ToEventMetadata(metadata));
        object IEventAdapter.Adapt(byte[] eventData, IEventMetadata eventMeta) => this.Adapt(eventData, (TMeta)eventMeta);

        public abstract EventData Adapt(object message, TMeta eventMeta);

        public abstract object Adapt(byte[] eventData, TMeta eventMeta);

        public abstract IEventMetadata ToEventMetadata(Dictionary<string, object> context);

        public abstract IEventMetadata ToEventMetadata(byte[] metadata);
    }
}
