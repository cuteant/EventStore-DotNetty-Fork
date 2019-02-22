using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#if NET451
using System.Reflection;
#endif

namespace EventStore.ClientAPI.Common.Utils.Threading
{
    internal static class TaskCompletionSourceFactory
    {
#if NET451
        static readonly FieldInfo StateField = typeof(Task).GetField("m_stateFlags", BindingFlags.NonPublic | BindingFlags.Instance);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TaskCompletionSource<T> Create<T>(TaskCreationOptions options = TaskCreationOptions.None)
        {
#if NET451
            //This lovely hack forces the task scheduler to run continuations asynchronously,
            //see https://stackoverflow.com/questions/22579206/how-can-i-prevent-synchronous-continuations-on-a-task/22588431#22588431
            var tcs = new TaskCompletionSource<T>(options);
            const int TASK_STATE_THREAD_WAS_ABORTED = 134217728;
            StateField.SetValue(tcs.Task, (int)StateField.GetValue(tcs.Task) | TASK_STATE_THREAD_WAS_ABORTED);
            return tcs;
#else
            return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }
    }
}