using System;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI.Common.Utils;

namespace EventStore.ClientAPI
{
    internal static class SubscriptionSettingsExtensions
    {
        /// <summary>Creates a <see cref="DataflowBlockOptions"/> object.</summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static DataflowBlockOptions ToBufferBlockOptions(this SubscriptionSettings settings)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }

            return new DataflowBlockOptions
            {
                TaskScheduler = TaskUtility.TaskSchedulerPair.ConcurrentScheduler,

                CancellationToken = settings.CancellationToken,
            };
        }

        /// <summary>Creates a <see cref="DataflowBlockOptions"/> object.</summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static DataflowBlockOptions ToDataflowBlockOptions(this SubscriptionSettings settings)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }

            return new DataflowBlockOptions
            {
                TaskScheduler = TaskUtility.TaskSchedulerPair.ConcurrentScheduler,

                CancellationToken = settings.CancellationToken,
                MaxMessagesPerTask = settings.MaxMessagesPerTask,
                BoundedCapacity = settings.BoundedCapacityPerBlock
            };
        }

        /// <summary>Creates a <see cref="ExecutionDataflowBlockOptions"/> object.</summary>
        /// <param name="settings"></param>
        /// <param name="singleProducerConstrained"></param>
        /// <returns></returns>
        public static ExecutionDataflowBlockOptions ToExecutionDataflowBlockOptions(this SubscriptionSettings settings, bool singleProducerConstrained = false)
        {
            if (null == settings) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings); }

            return new ExecutionDataflowBlockOptions
            {
                TaskScheduler = TaskUtility.TaskSchedulerPair.ConcurrentScheduler,

                CancellationToken = settings.CancellationToken,
                MaxMessagesPerTask = settings.MaxMessagesPerTask,
                BoundedCapacity = settings.BoundedCapacityPerBlock,

                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelismPerBlock,
                SingleProducerConstrained = singleProducerConstrained,
            };
        }
    }
}
