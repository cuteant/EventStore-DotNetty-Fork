using System;
using System.Collections.Generic;
using CuteAnt;

namespace EventStore.ClientAPI.Resilience
{
  /// <summary>Class that represents the retry policy for dealing with a dropped subscription.</summary>
  public class RetryPolicy
  {
    public const Int32 Unbounded = -1;

    private readonly TimeSpan _minDelay;
    private readonly TimeSpan _maxDelay;
    private readonly TimeSpan _step;
    private readonly double _powerFactor;

    public RetryPolicy(int maxNoOfRetries)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      _minDelay = TimeSpan.Zero; _maxDelay = TimeSpan.Zero; _step = TimeSpan.Zero;
      MaxNoOfRetries = maxNoOfRetries;
      ProviderType = SleepDurationProviderType.Immediately;
    }

    public RetryPolicy(int maxNoOfRetries, TimeSpan sleepDuration)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      _minDelay = TimeSpan.Zero; _maxDelay = TimeSpan.Zero; _step = TimeSpan.Zero;
      MaxNoOfRetries = maxNoOfRetries;
      ProviderType = SleepDurationProviderType.FixedDuration;
      SleepDurationProvider = retryAttempt => sleepDuration;
    }

    public RetryPolicy(int maxNoOfRetries, TimeSpan minDelay, TimeSpan maxDelay, TimeSpan step, double powerFactor)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      if (minDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "ExponentialBackoff min delay must be a positive number.");
      if (maxDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "ExponentialBackoff max delay must be a positive number.");
      if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step), step, "ExponentialBackoff step must be a positive number.");
      if (minDelay >= maxDelay) throw new ArgumentOutOfRangeException(nameof(minDelay), minDelay, "ExponentialBackoff min delay must be greater than max delay.");
      if (powerFactor <= 1.0D) { powerFactor = 2D; }

      _minDelay = minDelay;
      _maxDelay = maxDelay;
      _step = step;
      _powerFactor = powerFactor;

      MaxNoOfRetries = maxNoOfRetries;
      ProviderType = SleepDurationProviderType.ExponentialDuration;
      SleepDurationProvider = retryAttempt => Next(retryAttempt);
    }

    public RetryPolicy(int maxNoOfRetries, Func<int, TimeSpan> sleepDurationProvider)
    {
      if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { throw new ArgumentOutOfRangeException(nameof(maxNoOfRetries)); }
      if (null == sleepDurationProvider) { throw new ArgumentNullException(nameof(sleepDurationProvider)); }

      MaxNoOfRetries = maxNoOfRetries;
      ProviderType = SleepDurationProviderType.ExponentialDuration;
      SleepDurationProvider = sleepDurationProvider;
    }

    public SleepDurationProviderType ProviderType { get; }

    public int MaxNoOfRetries { get; }

    public Func<int, TimeSpan> SleepDurationProvider { get; }

    private TimeSpan Next(int attempt)
    {
      TimeSpan currMax;

      try
      {
        var multiple = Math.Pow(_powerFactor, attempt);
        currMax = _minDelay + _step.Multiply(multiple); // may throw OverflowException
        if (currMax <= TimeSpan.Zero) { throw new OverflowException(); }
      }
      catch { currMax = _maxDelay; }
      currMax = Ticks.Min(currMax, _maxDelay);
      return currMax;
    }

  }
}
