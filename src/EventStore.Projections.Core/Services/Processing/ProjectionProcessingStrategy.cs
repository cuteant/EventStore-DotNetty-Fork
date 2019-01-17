using System;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using EventStore.Core.Bus;
using EventStore.Core.Helpers;
using EventStore.Core.Services.TimerService;
using EventStore.Projections.Core.Messages;

namespace EventStore.Projections.Core.Services.Processing
{
    public abstract class ProjectionProcessingStrategy 
    {
        protected readonly string _name;
        protected readonly ProjectionVersion _projectionVersion;
        protected readonly ILogger _logger;

        protected ProjectionProcessingStrategy(string name, in ProjectionVersion projectionVersion, ILogger logger)
        {
            _name = name;
            _projectionVersion = projectionVersion;
            _logger = logger;
        }

        public CoreProjection Create(
            Guid projectionCorrelationId,
            IPublisher inputQueue,
            Guid workerId,
            IPrincipal runAs,
            IPublisher publisher,
            IODispatcher ioDispatcher,
            ReaderSubscriptionDispatcher subscriptionDispatcher,
            ITimeProvider timeProvider)
        {
            if (null == inputQueue) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inputQueue); }
            //if (null == runAs) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.runAs); }
            if (publisher == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.publisher);
            if (ioDispatcher == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.ioDispatcher);
            if (null == timeProvider) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.timeProvider); }

            var namingBuilder = new ProjectionNamesBuilder(_name, GetSourceDefinition());

            var coreProjectionCheckpointWriter =
                new CoreProjectionCheckpointWriter(
                    namingBuilder.MakeCheckpointStreamName(),
                    ioDispatcher,
                    _projectionVersion,
                    namingBuilder.EffectiveProjectionName);

            var partitionStateCache = new PartitionStateCache();

            return new CoreProjection(
                this,
                _projectionVersion,
                projectionCorrelationId,
                inputQueue,
                workerId,
                runAs,
                publisher,
                ioDispatcher,
                subscriptionDispatcher,
                _logger,
                namingBuilder,
                coreProjectionCheckpointWriter,
                partitionStateCache,
                namingBuilder.EffectiveProjectionName,
                timeProvider);
        }

        protected abstract IQuerySources GetSourceDefinition();

        public abstract bool GetStopOnEof();
        public abstract bool GetUseCheckpoints();
        public abstract bool GetRequiresRootPartition();
        public abstract bool GetProducesRunningResults();
        public abstract bool GetIsSlaveProjection();
        public abstract void EnrichStatistics(ProjectionStatistics info);

        public abstract IProjectionProcessingPhase[] CreateProcessingPhases(
            IPublisher publisher,
            IPublisher inputQueue,
            Guid projectionCorrelationId,
            PartitionStateCache partitionStateCache,
            Action updateStatistics,
            CoreProjection coreProjection,
            ProjectionNamesBuilder namingBuilder,
            ITimeProvider timeProvider,
            IODispatcher ioDispatcher,
            CoreProjectionCheckpointWriter coreProjectionCheckpointWriter);

        public abstract SlaveProjectionDefinitions GetSlaveProjections();
    }
}
