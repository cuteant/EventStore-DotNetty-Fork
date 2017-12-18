using System;
using System.Collections.Generic;

namespace EventStore.Core.Data
{
    public enum VNodeState
    {
        Initializing,
        Unknown,
        PreReplica,
        CatchingUp,
        Clone,
        Slave,
        PreMaster,
        Master,
        Manager,
        ShuttingDown,
        Shutdown
    }

    internal sealed class VNodeStateHelper
    {
        private static Dictionary<VNodeState, string> _lowerCaseMap;

        static VNodeStateHelper()
        {
            _lowerCaseMap = new Dictionary<VNodeState, string>()
            {
                [VNodeState.Initializing] = "initializing",
                [VNodeState.Unknown] = "unknown",
                [VNodeState.PreReplica] = "prereplica",
                [VNodeState.CatchingUp] = "catchingup",
                [VNodeState.Clone] = "clone",
                [VNodeState.Slave] = "slave",
                [VNodeState.PreMaster] = "premaster",
                [VNodeState.Master] = "master",
                [VNodeState.Manager] = "manager",
                [VNodeState.ShuttingDown] = "shuttingdown",
                [VNodeState.Shutdown] = "shutdown"
            };
        }

        internal static string ConvertToLower(VNodeState state)
        {
            if (_lowerCaseMap.TryGetValue(state, out string v))
            {
                return v;
            }
            else
            {
                return state.ToString().ToLowerInvariant();
            }
        }
    }

    public static class VNodeStateExtensions
    {
        public static bool IsReplica(this VNodeState state)
        {
            return state == VNodeState.CatchingUp
                || state == VNodeState.Clone
                || state == VNodeState.Slave;
        }
    }
}