using System;
using System.Collections.Generic;
using EventStore.Core.Bus;
using EventStore.Core.Messaging;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Messages.ParallelQueryProcessingMessages;
using Microsoft.Extensions.Logging;

namespace EventStore.Projections.Core.Services.Management
{
    public class ProjectionManagerMessageDispatcher
      : IHandle<PartitionProcessingResultBase>,
        IHandle<ReaderSubscriptionManagement.SpoolStreamReading>,
        IHandle<CoreProjectionManagementControlMessage>,
        IHandle<PartitionProcessingResultOutputBase>
    {
        private readonly ILogger _logger = TraceLogger.GetLogger<ProjectionManager>();
        private readonly IDictionary<Guid, IPublisher> _queueMap;

        public ProjectionManagerMessageDispatcher(IDictionary<Guid, IPublisher> queueMap)
        {
            _queueMap = queueMap;
        }

        public void Handle(PartitionProcessingResultBase message)
        {
            DispatchWorkerMessage(message, message.WorkerId);
        }

        public void Handle(ReaderSubscriptionManagement.SpoolStreamReading message)
        {
            DispatchWorkerMessage(
                new ReaderSubscriptionManagement.SpoolStreamReadingCore(
                    message.SubscriptionId,
                    message.StreamId,
                    message.CatalogSequenceNumber,
                    message.LimitingCommitPosition),
                message.WorkerId);
        }

        public void Handle(CoreProjectionManagementControlMessage message)
        {
            DispatchWorkerMessage(message, message.WorkerId);
        }

        private void DispatchWorkerMessage(Message message, Guid workerId)
        {
            IPublisher worker;
            if (_queueMap.TryGetValue(workerId, out worker))
            {
                worker.Publish(message);
            }
            else
            {
                if (_logger.IsInformationLevelEnabled()) _logger.LogInformation($"Cannot find a worker with ID: {workerId}");
            }
        }

        public void Handle(PartitionProcessingResultOutputBase message)
        {
            DispatchWorkerMessage(message.AsInput(), message.WorkerId);
        }
    }
}