using System;
using System.Threading.Tasks;
using CuteAnt;

namespace EventStore.ClientAPI.Common.Utils
{
  public static class TaskUtility
  {
    // The default concurrency level is DEFAULT_CONCURRENCY_MULTIPLIER * #CPUs. The higher the
    // DEFAULT_CONCURRENCY_MULTIPLIER, the more concurrent writes can take place without interference
    // and blocking, but also the more expensive operations that require all locks become (e.g. table
    // resizing, ToArray, Count, etc). According to brief benchmarks that we ran, 4 seems like a good
    // compromise.
    private const Int32 DEFAULT_CONCURRENCY_MULTIPLIER = 4;

    /// <summary>The number of concurrent writes for which to optimize by default.</summary>
    private static Int32 DefaultConcurrencyLevel => DEFAULT_CONCURRENCY_MULTIPLIER * PlatformHelper.ProcessorCount;

    /// <summary>TaskSchedulerPair</summary>
    public static ConcurrentExclusiveSchedulerPair TaskSchedulerPair => _taskSchedulerPair;

    private static readonly ConcurrentExclusiveSchedulerPair _taskSchedulerPair;

    static TaskUtility()
    {
      _taskSchedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, DefaultConcurrencyLevel);
    }

    internal static TaskCompletionSource<T> CreateTaskCompletionSource<T>()
    {
#if NET_4_5_GREATER
      return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#else
      return new TaskCompletionSource<T>();
#endif
    }
  }
}
