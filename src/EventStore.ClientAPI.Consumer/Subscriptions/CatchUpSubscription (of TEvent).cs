
namespace EventStore.ClientAPI.Subscriptions
{
    /// <summary>Represents a catch-up subscription to EventStore.</summary>
    public class CatchUpSubscription<TEvent> : Subscription<CatchUpSubscription<TEvent>, CatchUpSubscriptionSettings, TEvent>
    {
        public CatchUpSubscription() : base() { }

        /// <summary>Only for CatchUpConsumer</summary>
        public long? LastCheckpoint { get; set; }
    }
}
