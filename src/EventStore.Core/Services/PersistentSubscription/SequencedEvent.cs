using EventStore.Core.Data;

namespace EventStore.Core.Services.PersistentSubscription
{
    public readonly struct SequencedEvent
    {
        public readonly long Sequence;
        public readonly ResolvedEvent Event;

        public SequencedEvent(long sequence, in ResolvedEvent @event)
        {
            this.Sequence = sequence;
            this.Event = @event;
        }
    }
}