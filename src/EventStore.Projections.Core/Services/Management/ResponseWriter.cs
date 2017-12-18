using System;
using System.Collections.Generic;
using System.Linq;
using CuteAnt.Buffers;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Helpers;
using EventStore.Core.Messages;
using EventStore.Core.Services.UserManagement;
using EventStore.Projections.Core.Services.Processing;
using EventStore.Projections.Core.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.Projections.Core.Services.Management
{
    public sealed class ResponseWriter : IResponseWriter
    {
        private readonly IODispatcher _ioDispatcher;
        private readonly ILogger _logger = TraceLogger.GetLogger<ResponseWriter>();

        private bool Busy;
        private readonly List<Item> Items = new List<Item>();
        private IODispatcherAsync.CancellationScope _cancellationScope;

        private class Item
        {
            public string Command;
            public object Body;
        }

        public ResponseWriter(IODispatcher ioDispatcher)
        {
            _ioDispatcher = ioDispatcher;
            _cancellationScope = new IODispatcherAsync.CancellationScope();
        }

        public void Reset()
        {
            if (_logger.IsDebugLevelEnabled()) _logger.LogDebug("PROJECTIONS: Resetting Master Writer");
            _cancellationScope.Cancel();
            _cancellationScope = new IODispatcherAsync.CancellationScope();
            Items.Clear();
            Busy = false;
        }

        public void PublishCommand(string command, object body)
        {
            //TODO: PROJECTIONS: Remove before release
            if (!Logging.FilteredMessages.Contains(command) && _logger.IsDebugLevelEnabled())
            {
                _logger.LogDebug("PROJECTIONS: Scheduling the writing of {0} to {1}. Current status of Writer: Busy: {2}", command, ProjectionNamesBuilder._projectionsMasterStream, Busy);
            }
            Items.Add(new Item { Command = command, Body = body });
            if (!Busy)
            {
                EmitEvents();
            }
        }

        private void EmitEvents()
        {
            Busy = true;
            var events = Items.Select(CreateEvent).ToArray();
            Items.Clear();
            _ioDispatcher.BeginWriteEvents(
                _cancellationScope,
                ProjectionNamesBuilder._projectionsMasterStream,
                ExpectedVersion.Any,
                SystemAccount.Principal,
                events,
                completed =>
                {
                    Busy = false;
                    var debugEnabled = _logger.IsDebugLevelEnabled();
                    if (completed.Result == OperationResult.Success)
                    {
                        foreach (var evt in events)
                        {
                            //TODO: PROJECTIONS: Remove before release
                            if (!Logging.FilteredMessages.Contains(evt.EventType))
                            {
                                if (debugEnabled)
                                {
                                    _logger.LogDebug("PROJECTIONS: Finished writing events to {0}: {1}", ProjectionNamesBuilder._projectionsMasterStream, evt.EventType);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (debugEnabled)
                        {
                            var message = String.Format("PROJECTIONS: Failed writing events to {0} because of {1}: {2}",
                                ProjectionNamesBuilder._projectionsMasterStream,
                                completed.Result, String.Join(",", events.Select(x => String.Format("{0}-{1}", x.EventType, Helper.UTF8NoBom.GetStringWithBuffer(x.Data)))));
                            _logger.LogDebug(message); //Can't do anything about it, log and move on
                                                       //throw new Exception(message);
                        }
                    }

                    if (Items.Count > 0) { EmitEvents(); }
                }).Run();
        }

        private Event CreateEvent(Item item)
        {
            return new Event(Guid.NewGuid(), item.Command, true, item.Body.ToJsonBytes(), null);
        }
    }
}
