using System;
using EventStore.ClientAPI.Resilience;

namespace EventStore.ClientAPI.AutoSubscribing
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
  public class AutoSubscriberRetryPolicyAttribute : Attribute
  {
    public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      MinDelay = TimeSpan.Zero; MaxDelay = TimeSpan.Zero; Step = TimeSpan.Zero;
      MaxNoOfRetries = maxNoOfRetries;
      ProviderType = SleepDurationProviderType.Immediately;
    }

    public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries, TimeSpan sleepDuration)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      MinDelay = TimeSpan.Zero; MaxDelay = TimeSpan.Zero; Step = TimeSpan.Zero;
      MaxNoOfRetries = maxNoOfRetries;
      ProviderType = SleepDurationProviderType.FixedDuration;
      FixedSleepDuration = sleepDuration;
    }

    public AutoSubscriberRetryPolicyAttribute(int maxNoOfRetries, TimeSpan minDelay, TimeSpan maxDelay, TimeSpan step, double powerFactor)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != RetryPolicy.Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      if (minDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "ExponentialBackoff min delay must be a positive number.");
      if (maxDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "ExponentialBackoff max delay must be a positive number.");
      if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step), step, "ExponentialBackoff step must be a positive number.");
      if (minDelay >= maxDelay) throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "ExponentialBackoff min delay must be greater than max delay.");

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
