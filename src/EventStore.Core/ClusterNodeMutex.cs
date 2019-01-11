#if DESKTOPCLR
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
                if (Log.IsInformationLevelEnabled())
                {
                    Log.LogInformation(exc,
                                      "Cluster Node mutex '{0}' is said to be abandoned. "
                                      + "Probably previous instance of server was terminated abruptly.",
                                      MutexName);
                }
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
                using (Mutex.OpenExisting(mutexName, MutexRights.ReadPermissions))
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
                    Log.LogTrace(exc, "Exception while trying to open Cluster Node mutex '{0}': {1}.", mutexName, exc.Message);
                }
            }
            return false;
        }
    }
}
#endif
