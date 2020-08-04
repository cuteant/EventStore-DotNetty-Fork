using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Core.Bus;
using EventStore.Projections.Core.Messages;

namespace EventStore.Projections.Core.Services.Processing
{
    public abstract class WriteQueryResultProjectionProcessingPhaseBase : IProjectionProcessingPhase
    {
        private readonly IPublisher _publisher;
        private readonly int _phase;
        protected readonly string _resultStream;
        private readonly ICoreProjectionForProcessingPhase _coreProjection;
        protected readonly PartitionStateCache _stateCache;
        protected readonly ICoreProjectionCheckpointManager _checkpointManager;
        protected readonly IEmittedEventWriter _emittedEventWriter;
        protected readonly IEmittedStreamsTracker _emittedStreamsTracker;
        private bool _subscribed;
        private PhaseState _projectionState;

        public WriteQueryResultProjectionProcessingPhaseBase(
            IPublisher publisher,
            int phase,
            string resultStream,
            ICoreProjectionForProcessingPhase coreProjection,
            PartitionStateCache stateCache,
            ICoreProjectionCheckpointManager checkpointManager,
            IEmittedEventWriter emittedEventWriter,
            IEmittedStreamsTracker emittedStreamsTracker)
        {
            if (string.IsNullOrEmpty(resultStream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultStream); }
            if (null == coreProjection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.coreProjection); }
            if (null == stateCache) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stateCache); }
            if (null == checkpointManager) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.checkpointManager); }
            if (null == emittedEventWriter) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.emittedEventWriter); }
            if (null == emittedStreamsTracker) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.emittedStreamsTracker); }

            _publisher = publisher;
            _phase = phase;
            _resultStream = resultStream;
            _coreProjection = coreProjection;
            _stateCache = stateCache;
            _checkpointManager = checkpointManager;
            _emittedEventWriter = emittedEventWriter;
            _emittedStreamsTracker = emittedStreamsTracker;
        }

        public ICoreProjectionCheckpointManager CheckpointManager
        {
            get { return _checkpointManager; }
        }

        public IEmittedStreamsTracker EmittedStreamsTracker
        {
            get { return _emittedStreamsTracker; }
        }

        public void Dispose()
        {
        }

        public void Handle(CoreProjectionManagementMessage.GetState message)
        {
            var state = _stateCache.TryGetPartitionState(message.Partition);
            var stateString = state is object ? state.State : null;
            _publisher.Publish(
                new CoreProjectionStatusMessage.StateReport(
                    message.CorrelationId,
                    message.CorrelationId,
                    message.Partition,
                    state: stateString,
                    position: null));
        }

        public void Handle(CoreProjectionManagementMessage.GetResult message)
        {
            var state = _stateCache.TryGetPartitionState(message.Partition);
            var resultString = state is object ? state.Result : null;
            _publisher.Publish(
                new CoreProjectionStatusMessage.ResultReport(
                    message.CorrelationId,
                    message.CorrelationId,
                    message.Partition,
                    result: resultString,
                    position: null));

        }

        public void Handle(CoreProjectionProcessingMessage.PrerecordedEventsLoaded message)
        {
            throw new NotImplementedException();
        }

        public CheckpointTag AdjustTag(CheckpointTag tag)
        {
            return tag;
        }

        public void InitializeFromCheckpoint(CheckpointTag checkpointTag)
        {
            _subscribed = false;
        }

        public void AssignSlaves(SlaveProjectionCommunicationChannels slaveProjections)
        {
            // intentionally ignored 
        }

        public void ProcessEvent()
        {
            if (!_subscribed)
                throw new InvalidOperationException();
            if (_projectionState != PhaseState.Running)
                return;

            var phaseCheckpointTag = CheckpointTag.FromPhase(_phase, completed: true);
            var writeResults = WriteResults(phaseCheckpointTag);

            var writeEofResults = WriteEofEvent(phaseCheckpointTag);

            _emittedEventWriter.EventsEmitted(writeResults.Concat(writeEofResults).ToArray(), Guid.Empty, null);

            _checkpointManager.EventProcessed(phaseCheckpointTag, 100.0f);
            _coreProjection.CompletePhase();
        }

        private IEnumerable<EmittedEventEnvelope> WriteEofEvent(CheckpointTag phaseCheckpointTag)
        {
            EmittedStream.WriterConfiguration.StreamMetadata streamMetadata = null;
            yield return
                new EmittedEventEnvelope(
                    new EmittedDataEvent(
                        _resultStream,
                        Guid.NewGuid(),
                        "$Eof",
                        true,
                        null,
                        null,
                        phaseCheckpointTag,
                        null),
                    streamMetadata);
        }

        protected abstract IEnumerable<EmittedEventEnvelope> WriteResults(CheckpointTag phaseCheckpointTag);

        public void Subscribe(CheckpointTag from, bool fromCheckpoint)
        {
            _subscribed = true;
            _coreProjection.Subscribed();
        }

        public void SetProjectionState(PhaseState state)
        {
            _projectionState = state;
        }

        public void GetStatistics(ProjectionStatistics info)
        {
            info.Status = info.Status + "/Writing results";
        }

        public CheckpointTag MakeZeroCheckpointTag()
        {
            return CheckpointTag.FromPhase(_phase, completed: false);
        }

        public void EnsureUnsubscribed()
        {
        }
    }
}