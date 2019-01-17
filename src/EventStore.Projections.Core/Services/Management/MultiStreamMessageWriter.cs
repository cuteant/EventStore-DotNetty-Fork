using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Helpers;
using EventStore.Core.Messages;
using EventStore.Core.Services.UserManagement;
using EventStore.Projections.Core.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.Projections.Core.Services.Management
{
    public sealed class MultiStreamMessageWriter : IMultiStreamMessageWriter
    {
        private readonly ILogger Log = TraceLogger.GetLogger<MultiStreamMessageWriter>();
        private readonly IODispatcher _ioDispatcher;

        private readonly Dictionary<Guid, Queue> _queues = new Dictionary<Guid, Queue>();
        private IODispatcherAsync.CancellationScope _cancellationScope;

        public MultiStreamMessageWriter(IODispatcher ioDispatcher)
        {
            _ioDispatcher = ioDispatcher;
            _cancellationScope = new IODispatcherAsync.CancellationScope();
        }

        public void Reset()
        {
            if (Log.IsDebugLevelEnabled()) Log.ResettingWorkerWriter();
            _cancellationScope.Cancel();
            _cancellationScope = new IODispatcherAsync.CancellationScope();
            _queues.Clear();
        }

        public void PublishResponse(string command, Guid workerId, object body)
        {
            if (!_queues.TryGetValue(workerId, out Queue queue))
            {
                queue = new Queue();
                _queues.Add(workerId, queue);
            }
            //TODO: PROJECTIONS: Remove before release
            if (!Logging.FilteredMessages.Contains(command) && Log.IsDebugLevelEnabled())
            {
                Log.ProjectionsSchedulingTheWritingOf(command, workerId, queue.Busy);
            }
            queue.Items.Add(new Queue.Item { Command = command, Body = body });
            if (!queue.Busy)
            {
                EmitEvents(queue, workerId);
            }
        }

        private void EmitEvents(Queue queue, Guid workerId)
        {
            queue.Busy = true;
            var events = queue.Items.Select(CreateEvent).ToArray();
            queue.Items.Clear();
            var streamId = "$projections-$" + workerId.ToString("N");
            _ioDispatcher.BeginWriteEvents(
                _cancellationScope,
                streamId,
                ExpectedVersion.Any,
                SystemAccount.Principal,
                events,
                completed =>
                {
                    queue.Busy = false;
                    if (completed.Result == OperationResult.Success)
                    {
                        foreach (var evt in events)
                        {
                            // TODO: PROJECTIONS: Remove before release
                            if (Log.IsDebugLevelEnabled() && !Logging.FilteredMessages.Contains(evt.EventType))
                            {
                                Log.ProjectionsFinishedWritingEventsTo(streamId, evt.EventType);
                            }
                        }
                    }
                    else
                    {
                        if (Log.IsDebugLevelEnabled())
                        {
                            Log.ProjectionsFailedWritingEventsTo(streamId, completed.Result, events); //Can't do anything about it, log and move on
                        }
                        //throw 1new 1Exception(message);
                    }

                    if (queue.Items.Count > 0)
                        EmitEvents(queue, workerId);
                    else
                        _queues.Remove(workerId);
                }).Run();
        }

        private Event CreateEvent(Queue.Item item)
        {
            return new Event(Guid.NewGuid(), item.Command, true, item.Body.ToJsonBytes(), null);
        }

        private class Queue
        {
            public bool Busy;
            public readonly List<Item> Items = new List<Item>();

            internal class Item
            {
                public string Command;
                public object Body;
            }
        }
    }
}
