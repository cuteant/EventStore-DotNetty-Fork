using System;
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
            if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            _minDelay = TimeSpan.Zero; _maxDelay = TimeSpan.Zero; _step = TimeSpan.Zero;
            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.Immediately;
        }

        public RetryPolicy(int maxNoOfRetries, TimeSpan sleepDuration)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            _minDelay = TimeSpan.Zero; _maxDelay = TimeSpan.Zero; _step = TimeSpan.Zero;
            MaxNoOfRetries = maxNoOfRetries;
            ProviderType = SleepDurationProviderType.FixedDuration;
            SleepDurationProvider = retryAttempt => sleepDuration;
        }

        public RetryPolicy(int maxNoOfRetries, TimeSpan minDelay, TimeSpan maxDelay, TimeSpan step, double powerFactor)
        {
            if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            if (minDelay <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMin(minDelay); }
            if (maxDelay <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMax(maxDelay); }
            if (step <= TimeSpan.Zero) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffStep(step); }
            if (minDelay >= maxDelay) { ThrowHelper.ThrowArgumentOutOfRangeException_ExponentialBackoffMinMax(minDelay); }
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
            if (maxNoOfRetries < 1 && maxNoOfRetries != Unbounded) { ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxNoOfRetries); }
            if (null == sleepDurationProvider) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sleepDurationProvider); }

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
                if (currMax <= TimeSpan.Zero) { ThrowHelper.ThrowOverflowException(); }
            }
            catch { currMax = _maxDelay; }
            currMax = Ticks.Min(currMax, _maxDelay);
            return currMax;
        }

    }
}
