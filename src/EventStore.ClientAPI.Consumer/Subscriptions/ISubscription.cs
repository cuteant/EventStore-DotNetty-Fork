using EventStore.ClientAPI.Resilience;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Subscriptions
{
    public interface ISubscription
    {
        string StreamId { get; }
        string SubscriptionId { get; }
        string Topic { get; set; }

        StreamMetadata StreamMeta { get; }

        RetryPolicy RetryPolicy { get; set; }

        UserCredentials Credentials { get; set; }
    }

    public interface ISubscription<TSettings> : ISubscription where TSettings : SubscriptionSettings
    {
        TSettings Settings { get; set; }
    }
}