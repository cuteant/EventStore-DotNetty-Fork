using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EventStore.Common.Utils
{
    public enum ExitCode
    {
        Success = 0,
        Error = 1
    }

    public class Application
    {
        public const string AdditionalCommitChecks = "ADDITIONAL_COMMIT_CHECKS";
        public const string InfiniteMetastreams = "INFINITE_METASTREAMS";
        public const string DumpStatistics = "DUMP_STATISTICS";
        public const string DoNotTimeoutRequests = "DO_NOT_TIMEOUT_REQUESTS";
        public const string AlwaysKeepScavenged = "ALWAYS_KEEP_SCAVENGED";
        public const string DisableMergeChunks = "DISABLE_MERGE_CHUNKS";

        protected static readonly ILogger Log = TraceLogger.GetLogger<Application>();

        private static Action<int> _exit;
        private static int _exited;

        private static readonly HashSet<string> _defines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void RegisterExitAction(Action<int> exitAction)
        {
            if (exitAction is null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exitAction);

            _exit = exitAction;
        }

        public static void ExitSilent(int exitCode, string reason)
        {
            Exit(exitCode, reason, silent: true);
        }

        public static void Exit(ExitCode exitCode, string reason)
        {
            Exit((int)exitCode, reason);
        }

        public static void Exit(int exitCode, string reason)
        {
            Exit(exitCode, reason, silent: false);
        }

        private static void Exit(int exitCode, string reason, bool silent)
        {
            if (Interlocked.CompareExchange(ref _exited, 1, 0) != 0) { return; }

            if (string.IsNullOrEmpty(reason)) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.reason);

            if (!silent)
            {
                var message = $"Exiting with exit code: {exitCode}.\nExit reason: {reason}";
                if (Log.IsInformationLevelEnabled()) Log.LogInformation(message);
                if (exitCode != 0)
                {
                    Log.LogError(message);
                }
                else
                {
                    if (Log.IsInformationLevelEnabled()) Log.LogInformation(message);
                }
            }

            var exit = _exit;
            exit?.Invoke(exitCode);
        }

        public static void AddDefines(IEnumerable<string> defines)
        {
            foreach (var define in defines.Safe())
            {
                _defines.Add(define);
            }
        }

        public static bool IsDefined(string define)
        {
            if (null == define) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.define);
            return _defines.Contains(define);
        }
    }
}