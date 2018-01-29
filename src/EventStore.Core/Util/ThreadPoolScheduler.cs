// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    internal static class ThreadPoolScheduler
    {
        public static void Schedule(Action action)
        {
#if NETCOREAPP_2_0_GREATER
            // Queue to low contention local ThreadPool queue; rather than global queue as per Task
            ThreadPool.QueueUserWorkItem(_actionWaitCallback, action, preferLocal: true);
#else
            ThreadPool.QueueUserWorkItem(_actionWaitCallback, action);
#endif
        }

        public static void Schedule(Action<object> action, object state)
        {
#if NETCOREAPP_2_0_GREATER
            // Queue to low contention local ThreadPool queue; rather than global queue as per Task
            ThreadPool.QueueUserWorkItem(_actionObjectWaitCallback, new ActionObjectAsWaitCallback(action, state), preferLocal: true);
#else
            ThreadPool.QueueUserWorkItem(_actionObjectWaitCallback, new ActionObjectAsWaitCallback(action, state));
#endif
        }

        private readonly static WaitCallback _actionWaitCallback = state => ((Action)state)();

        private readonly static WaitCallback _actionObjectWaitCallback = state => ((ActionObjectAsWaitCallback)state).Run();

        private sealed class ActionObjectAsWaitCallback
        {
            private Action<object> _action;
            private object _state;

            public ActionObjectAsWaitCallback(Action<object> action, object state)
            {
                _action = action;
                _state = state;
            }

            public void Run() => _action(_state);
        }
    }
}
