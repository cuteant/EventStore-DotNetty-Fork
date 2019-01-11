using System;
using EventStore.ClientAPI.Resilience;

namespace EventStore.ClientAPI.AutoSubscribing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public class AutoSubscriberRetryPolicyAttribute : Attribute
    {
        public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            MinDelay = TimeSpan.Zero; MaxDelay = TimeSpan.Zero; Step = TimeSpan.Zero;
            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.Immediately;
        }

        public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries, TimeSpan sleepDuration)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            MinDelay = TimeSpan.Zero; MaxDelay = TimeSpan.Zero; Step = TimeSpan.Zero;
            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.FixedDuration;
            FixedSleepDuration = sleepDuration;
        }

        public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries, TimeSpan minDelay, TimeSpan maxDelay, TimeSpan step, double powerFactor)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            if (minDelay <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMin(minDelay); }
            if (maxDelay <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMax(maxDelay); }
            if (step <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffStep(step); }
            if (minDelay >= maxDelay) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMinMax(minDelay); }

            MinDelay = minDelay;
            MaxDelay = maxDelay;
            Step = step;
            PowerFactor = powerFactor;

            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.ExponentialDuration;
        }

        public SleepDurationProviderType ProviderType { get; }

        public int MaxNoOfRetries { get; }

        public TimeSpan FixedSleepDuration { get; }

        public TimeSpan MinDelay { get; }
        public TimeSpan MaxDelay { get; }
        public TimeSpan Step { get; }
        public double PowerFactor { get; }
    }
}
