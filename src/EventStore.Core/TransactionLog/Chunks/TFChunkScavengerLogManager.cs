using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Helpers;
using EventStore.Core.Messages;
using EventStore.Core.Services;
using EventStore.Core.Services.Storage;
using EventStore.Core.Services.UserManagement;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.TransactionLog.Chunks
{
    public class TFChunkScavengerLogManager : ITFChunkScavengerLogManager
    {
        private readonly string _nodeEndpoint;
        private readonly TimeSpan _scavengeHistoryMaxAge;
        private readonly IODispatcher _ioDispatcher;
        private const int MaxRetryCount = 5;
        private static readonly ILogger Log = TraceLogger.GetLogger<StorageScavenger>();
        private int _isInitialised;

        public TFChunkScavengerLogManager(string nodeEndpoint, TimeSpan scavengeHistoryMaxAge, IODispatcher ioDispatcher)
        {
            _nodeEndpoint = nodeEndpoint;
            _scavengeHistoryMaxAge = scavengeHistoryMaxAge;
            _ioDispatcher = ioDispatcher;
        }

        public void Initialise()
        {
            // We only initialise on first election so we don't incorrectly mark running scavenges as interrupted.
            if (Interlocked.Exchange(ref _isInitialised, 1) != 0)
                return;

            SetScavengeStreamMetadata();

            if (Log.IsDebugLevelEnabled()) Log.Searching_for_incomplete_scavenges_on_node(_nodeEndpoint);
            GatherIncompleteScavenges(-1, new HashSet<string>(), new List<string>(), new List<string>());
        }

        public ITFChunkScavengerLog CreateLog()
        {
            return CreateLogInternal(Guid.NewGuid().ToString());
        }

        private TFChunkScavengerLog CreateLogInternal(string scavengeId)
        {
            return new TFChunkScavengerLog(_ioDispatcher, scavengeId, _nodeEndpoint, MaxRetryCount, _scavengeHistoryMaxAge);
        }

        private void SetScavengeStreamMetadata()
        {
            var metaStreamId = SystemStreams.MetastreamOf(SystemStreams.ScavengesStream);

            _ioDispatcher.ReadBackward(metaStreamId, -1, 1, false, SystemAccount.Principal, readResult =>
            {
                if (readResult.Result == ReadStreamResult.Success || readResult.Result == ReadStreamResult.NoStream)
                {
                    if ((uint)readResult.Events.Length == 1u)
                    {
                        var currentMetadata = StreamMetadata.FromJsonBytes(readResult.Events[0].Event.Data);
                        var hasProperACL = currentMetadata.Acl is object
                                        && currentMetadata.Acl.ReadRoles is object
                                        && currentMetadata.Acl.ReadRoles.Contains(x => string.Equals("$ops", x));

                        if (currentMetadata.MaxAge == _scavengeHistoryMaxAge && hasProperACL)
                        {
                            if (Log.IsDebugLevelEnabled()) Log.Max_age_already_set_for_the_stream();
                            return;
                        }
                    }

                    if (Log.IsDebugLevelEnabled())
                    {
                        Log.Setting_max_age_for_the_stream_to(_scavengeHistoryMaxAge);
                        Log.Setting_ops_read_permission_for_the_scavenges_stream();
                    }

                    var acl = new StreamAcl(
                        new string[] { "$ops" },
                        new string[] { },
                        new string[] { },
                        new string[] { },
                        new string[] { }
                    );

                    var metadata = new StreamMetadata(maxAge: _scavengeHistoryMaxAge, acl: acl);
                    var metaStreamEvent = new Event(Guid.NewGuid(), SystemEventTypes.StreamMetadata, isJson: true, data: metadata.ToJsonBytes(), metadata: null);
                    _ioDispatcher.WriteEvent(metaStreamId, ExpectedVersion.Any, metaStreamEvent, SystemAccount.Principal, m =>
                    {
                        if (m.Result != OperationResult.Success)
                        {
                            Log.FailedToWriteTheMaxageOfDaysAndSetOpsPermissionForTheStream(_scavengeHistoryMaxAge.TotalDays, m.Result);
                        }
                    });

                }
            });
        }

        private void GatherIncompleteScavenges(long from, ISet<string> completedScavenges, IList<string> incompleteScavenges, IList<string> recentScavenges)
        {
            _ioDispatcher.ReadBackward(SystemStreams.ScavengesStream, from, 20, true, SystemAccount.Principal,
                readResult =>
                {
                    if (readResult.Result != ReadStreamResult.Success && readResult.Result != ReadStreamResult.NoStream)
                    {
                        if (Log.IsDebugLevelEnabled()) Log.Unable_to_read_for_scavenge_log_clean_up(readResult.Result);
                        return;
                    }

                    foreach (var ev in readResult.Events)
                    {
                        if (ev.ResolveResult == ReadEventResult.Success)
                        {
                            var dictionary = ev.Event.Data.ParseJson<Dictionary<string, object>>();

                            if (!dictionary.TryGetValue("nodeEndpoint", out object entryNode) || entryNode.ToString() != _nodeEndpoint)
                            {
                                continue;
                            }

                            if (!dictionary.TryGetValue("scavengeId", out object scavengeIdEntry))
                            {
                                if (Log.IsWarningLevelEnabled()) Log.An_entry_in_the_scavenge_log_has_no_scavengeId();
                                continue;
                            }

                            var scavengeId = scavengeIdEntry.ToString();
                            if (recentScavenges.Count <= 1000) //bound size
                                recentScavenges.Add(scavengeId);

                            if (ev.Event.EventType == SystemEventTypes.ScavengeCompleted)
                            {
                                completedScavenges.Add(scavengeId);
                            }
                            else if (ev.Event.EventType == SystemEventTypes.ScavengeStarted)
                            {
                                if (!completedScavenges.Contains(scavengeId))
                                {
                                    incompleteScavenges.Add(scavengeId);
                                }
                            }
                        }

                    }

                    if (readResult.IsEndOfStream || 0u >= (uint)readResult.Events.Length)
                    {
                        SetOpsPermissions(recentScavenges);
                        CompleteInterruptedScavenges(incompleteScavenges);
                    }
                    else
                    {
                        GatherIncompleteScavenges(readResult.NextEventNumber, completedScavenges, incompleteScavenges, recentScavenges);
                    }
                });
        }

        private void SetOpsPermissions(IList<string> recentScavengeIds)
        {
            //sets $ops permissions on last 30 $scavenges-<scavenge id> stream
            //added for backward compatibility to make UI scavenge history work properly with $ops users

            var last30ScavengeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var scavengeId in recentScavengeIds)
            {
                if (last30ScavengeIds.Count >= 30) { break; }
                last30ScavengeIds.Add(scavengeId);
            }

            var last30ScavengeIdCount = last30ScavengeIds.Count;
            if (last30ScavengeIdCount > 0 && Log.IsDebugLevelEnabled())
            {
                Log.Setting_ops_read_permission_on_last_scavenges_streams(last30ScavengeIdCount);
            }

            foreach (var scavengeId in last30ScavengeIds)
            {
                var acl = new StreamAcl(
                    new string[] { "$ops" },
                    new string[] { },
                    new string[] { },
                    new string[] { },
                    new string[] { }
                );

                var scavengeIdStream = SystemStreams.ScavengesStream + "-" + scavengeId;
                var metaStreamId = SystemStreams.MetastreamOf(scavengeIdStream);
                var metadata = new StreamMetadata(maxAge: _scavengeHistoryMaxAge, acl: acl);
                var metaStreamEvent = new Event(Guid.NewGuid(), SystemEventTypes.StreamMetadata, isJson: true,
                    data: metadata.ToJsonBytes(), metadata: null);
                _ioDispatcher.WriteEvent(metaStreamId, ExpectedVersion.Any, metaStreamEvent,
                    SystemAccount.Principal, m =>
                    {
                        if (m.Result != OperationResult.Success)
                        {
                            Log.FailedToSetOpsReadPermission(scavengeIdStream, m.Result);
                        }
                    });
            }
        }

        private void CompleteInterruptedScavenges(IList<string> incompletedScavenges)
        {
            if (0u >= (uint)incompletedScavenges.Count)
            {
                if (Log.IsDebugLevelEnabled()) Log.No_incomplete_scavenges_found_on_node(_nodeEndpoint);
            }
            else
            {
                if (Log.IsInformationLevelEnabled()) Log.Found_incomplete_scavenges_on_node(incompletedScavenges, _nodeEndpoint);
            }

            foreach (var incompletedScavenge in incompletedScavenges)
            {
                var log = CreateLogInternal(incompletedScavenge);

                log.ScavengeCompleted(ScavengeResult.Failed, "The node was restarted.", TimeSpan.Zero);
            }
        }
    }
}