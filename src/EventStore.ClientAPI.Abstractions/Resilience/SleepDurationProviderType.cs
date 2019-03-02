
namespace EventStore.ClientAPI.Resilience
{
    public enum SleepDurationProviderType
    {
        Immediately,
        FixedDuration,
        ExponentialDuration
    }
}
