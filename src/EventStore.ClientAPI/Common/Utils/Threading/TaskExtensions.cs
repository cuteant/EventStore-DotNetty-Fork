using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.ClientAPI.Common.Utils.Threading
{
    internal static class TaskExtensions
    {
        private static readonly Action<Task> IgnoreTaskContinuation = t => { var ignored = t.Exception; };

        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> taskToComplete, TimeSpan timeSpan)
        {
            //return await task;
            if (taskToComplete.IsCompleted)
            {
                return await taskToComplete;
            }

            var timeoutCancellationTokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(taskToComplete, Task.Delay(timeSpan, timeoutCancellationTokenSource.Token));

            if (taskToComplete != completedTask)
            {
                // We did not complete before the timeout, we fire and forget to ensure we observe any exceptions that may occur
                taskToComplete.Ignore();
                CoreThrowHelper.ThrowOperationTimedOutException(timeSpan);
            }

            // We got done before the timeout, or were able to complete before this code ran, return the result
            timeoutCancellationTokenSource.Cancel();
            // Await this so as to propagate the exception correctly
            return await taskToComplete;
        }

        internal static void Ignore(this Task task)
        {
            if (task.IsCompleted)
            {
                var ignored = task.Exception;
            }
            else
            {
                task.ContinueWith(
                    IgnoreTaskContinuation,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }
}
