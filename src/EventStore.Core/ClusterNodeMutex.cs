using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EventStore.Core
{
    public class ClusterNodeMutex
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<ClusterNodeMutex>();

        public readonly string MutexName;
        public bool IsAcquired { get { return _acquired; } }

        private Mutex _clusterNodeMutex;
        private bool _acquired;

        public ClusterNodeMutex()
        {
            MutexName = $"ESCLUSTERNODE:{Process.GetCurrentProcess().Id}";
        }

        public bool Acquire()
        {
            if (_acquired) ThrowHelper.ThrowInvalidOperationException_ClusterNodeMutexIsAlreadyAcquired(MutexName);

            try
            {
                _clusterNodeMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out _acquired);
            }
            catch (AbandonedMutexException exc)
            {
                if (Log.IsInformationLevelEnabled()) { Log.Cluster_Node_mutex_is_said_to_be_abando1ned(MutexName, exc); }
            }

            return _acquired;
        }

        public void Release()
        {
            if (!_acquired) ThrowHelper.ThrowInvalidOperationException_ClusterNodeMutexWasNotAcquired(MutexName);
            _clusterNodeMutex.ReleaseMutex();
        }

        public static bool IsPresent(int pid)
        {
            var mutexName = $"ESCLUSTERNODE:{pid}";
            try
            {
#if DESKTOPCLR
                using (Mutex.OpenExisting(mutexName, MutexRights.ReadPermissions))
#else
                using (Mutex.OpenExisting(mutexName))
#endif
                {
                    return true;
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
            catch (Exception exc)
            {
                if (Log.IsTraceLevelEnabled())
                {
                    Log.ExceptionWhileTryingToOpenClusterNodeMutex(exc, mutexName);
                }
            }
            return false;
        }
    }
}
