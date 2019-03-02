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
            MinDelay = "0ms"; MaxDelay = "0ms"; Step = "0ms";
            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.Immediately;
        }

        public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries, string sleepDuration)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            MinDelay = "0ms"; MaxDelay = "0ms"; Step = "0ms";
            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.FixedDuration;
            FixedSleepDuration = sleepDuration;
        }

        public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries, string minDelay, string maxDelay, string step, double powerFactor)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            //if (minDelay <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMin(minDelay); }
            //if (maxDelay <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMax(maxDelay); }
            //if (step <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffStep(step); }
            //if (minDelay >= maxDelay) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMinMax(minDelay); }

            MinDelay = minDelay;
            MaxDelay = maxDelay;
            Step = step;
            PowerFactor = powerFactor;

            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.ExponentialDuration;
        }

        public SleepDurationProviderType ProviderType { get; }

        public int MaxNoOfRetries { get; }

        public string FixedSleepDuration { get; }

        public string MinDelay { get; }
        public string MaxDelay { get; }
        public string Step { get; }
        public double PowerFactor { get; }
    }
}
