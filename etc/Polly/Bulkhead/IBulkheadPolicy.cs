using System;

namespace Polly.Bulkhead
{
  /// <summary>
  /// Defines properties and methods common to all bulkhead policies.
  /// </summary>

  public interface IBulkheadPolicy : IsPolicy, IDisposable
  {
    /// <summary>
    /// Gets the number of slots currently available for executing actions through the bulkhead.
    /// </summary>
#if NET40
    long BulkheadAvailableCount { get; }
#else
    int BulkheadAvailableCount { get; }
#endif

    /// <summary>
    /// Gets the number of slots currently available for queuing actions for execution through the bulkhead.
    /// </summary>
#if NET40
    long QueueAvailableCount { get; }
#else
    int QueueAvailableCount { get; }
#endif
  }

  /// <summary>
  /// Defines properties and methods common to all bulkhead policies generic-typed for executions returning results of type <typeparamref name="TResult"/>.
  /// </summary>
  public interface IBulkheadPolicy<TResult> : IBulkheadPolicy
  {

  }
}
