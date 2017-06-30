using System;
using System.Collections.Generic;

namespace EventStore.ClientAPI.Resilience
{
  /// <summary>Class that represents the retry policy for dealing with a dropped subscription.</summary>
  public class RetryPolicy
  {
    public RetryPolicy(params TimeSpan[] sleepDurations)
    {
      SleepDurations = sleepDurations;
      RetryType = RetryType.Durations;
    }

    public RetryType RetryType { get; }

    public RetryPolicy(Retries maxNoOfRetries, Func<int, TimeSpan> sleepDurationProvider)
    {
      MaxNoOfRetries = maxNoOfRetries;
      SleepDurationProvider = sleepDurationProvider;
      RetryType = RetryType.Provider;
    }

    public Retries MaxNoOfRetries { get; }

    public Func<int, TimeSpan> SleepDurationProvider { get; }

    public IEnumerable<TimeSpan> SleepDurations { get; }
  }
}
