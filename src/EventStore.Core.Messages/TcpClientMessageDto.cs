using System.Net;
using MessagePack;

namespace EventStore.Core.Messages
{
    public static partial class TcpClientMessageDto
    {
        [MessagePackObject]
        public sealed class NewEvent
        {
            [Key(0)]
            public readonly byte[] EventId;
            [Key(1)]
            public readonly string EventType;
            [Key(2)]
            public readonly int DataContentType;
            [Key(3)]
            public readonly int MetadataContentType;
            [Key(4)]
            public readonly byte[] Data;
            [Key(5)]
            public readonly byte[] Metadata;

            private NewEvent() { }

            [SerializationConstructor]
            public NewEvent(byte[] eventId, string eventType, int dataContentType, int metadataContentType, byte[] data, byte[] metadata)
            {
                EventId = eventId;
                EventType = eventType;
                DataContentType = dataContentType;
                MetadataContentType = metadataContentType;
                Data = data;
                Metadata = metadata;
            }
        }

        [MessagePackObject]
        public sealed partial class EventRecord
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly long EventNumber;
            [Key(2)]
            public readonly byte[] EventId;
            [Key(3)]
            public readonly string EventType;
            [Key(4)]
            public readonly int DataContentType;
            [Key(5)]
            public readonly int MetadataContentType;
            [Key(6)]
            public readonly byte[] Data;
            [Key(7)]
            public readonly byte[] Metadata;
            [Key(8)]
            public readonly long? Created;
            [Key(9)]
            public readonly long? CreatedEpoch;

            private EventRecord() { }

            [SerializationConstructor]
            public EventRecord(string eventStreamId, long eventNumber, byte[] eventId, string eventType, int dataContentType, int metadataContentType, byte[] data, byte[] metadata, long? created, long? createdEpoch)
            {
                EventStreamId = eventStreamId;
                EventNumber = eventNumber;
                EventId = eventId;
                EventType = eventType;
                DataContentType = dataContentType;
                MetadataContentType = metadataContentType;
                Data = data;
                Metadata = metadata;
                Created = created;
                CreatedEpoch = createdEpoch;
            }
        }

        [MessagePackObject]
        public sealed partial class ResolvedIndexedEvent
        {
            [Key(0)]
            public readonly EventRecord Event;
            [Key(1)]
            public readonly EventRecord Link;

            private ResolvedIndexedEvent() { }

            [SerializationConstructor]
            public ResolvedIndexedEvent(EventRecord @event, EventRecord link)
            {
                Event = @event;
                Link = link;
            }
        }

        [MessagePackObject]
        public sealed partial class ResolvedEvent
        {
            [Key(0)]
            public readonly EventRecord Event;
            [Key(1)]
            public readonly EventRecord Link;
            [Key(2)]
            public readonly long CommitPosition;
            [Key(3)]
            public readonly long PreparePosition;

            private ResolvedEvent() { }

            [SerializationConstructor]
            public ResolvedEvent(EventRecord @event, EventRecord link, long commitPosition, long preparePosition)
            {
                Event = @event;
                Link = link;
                CommitPosition = commitPosition;
                PreparePosition = preparePosition;
            }
        }

        [MessagePackObject]
        public sealed class WriteEvents
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly long ExpectedVersion;
            [Key(2)]
            public readonly NewEvent[] Events;
            [Key(3)]
            public readonly bool RequireMaster;

            private WriteEvents() { }

            [SerializationConstructor]
            public WriteEvents(string eventStreamId, long expectedVersion, NewEvent[] events, bool requireMaster)
            {
                EventStreamId = eventStreamId;
                ExpectedVersion = expectedVersion;
                Events = events;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class WriteEventsCompleted
        {
            [Key(0)]
            public readonly OperationResult Result;
            [Key(1)]
            public readonly string Message;
            [Key(2)]
            public readonly long FirstEventNumber;
            [Key(3)]
            public readonly long LastEventNumber;
            [Key(4)]
            public readonly long? PreparePosition;
            [Key(5)]
            public readonly long? CommitPosition;
            [Key(6)]
            public readonly long? CurrentVersion;

            private WriteEventsCompleted() { }

            [SerializationConstructor]
            public WriteEventsCompleted(OperationResult result, string message, long firstEventNumber, long lastEventNumber, long? preparePosition, long? commitPosition, long? currentVersion)
            {
                Result = result;
                Message = message;
                FirstEventNumber = firstEventNumber;
                LastEventNumber = lastEventNumber;
                PreparePosition = preparePosition;
                CommitPosition = commitPosition;
                CurrentVersion = currentVersion;
            }
        }

        [MessagePackObject]
        public sealed class DeleteStream
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly long ExpectedVersion;
            [Key(2)]
            public readonly bool RequireMaster;
            [Key(3)]
            public readonly bool? HardDelete;

            private DeleteStream() { }

            [SerializationConstructor]
            public DeleteStream(string eventStreamId, long expectedVersion, bool requireMaster, bool? hardDelete)
            {
                EventStreamId = eventStreamId;
                ExpectedVersion = expectedVersion;
                RequireMaster = requireMaster;
                HardDelete = hardDelete;
            }
        }

        [MessagePackObject]
        public sealed class DeleteStreamCompleted
        {
            [Key(0)]
            public readonly OperationResult Result;
            [Key(1)]
            public readonly string Message;
            [Key(2)]
            public readonly long? PreparePosition;
            [Key(3)]
            public readonly long? CommitPosition;

            private DeleteStreamCompleted() { }

            [SerializationConstructor]
            public DeleteStreamCompleted(OperationResult result, string message, long? preparePosition, long? commitPosition)
            {
                Result = result;
                Message = message;
                PreparePosition = preparePosition;
                CommitPosition = commitPosition;
            }
        }

        [MessagePackObject]
        public sealed class TransactionStart
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly long ExpectedVersion;
            [Key(2)]
            public readonly bool RequireMaster;

            private TransactionStart() { }

            [SerializationConstructor]
            public TransactionStart(string eventStreamId, long expectedVersion, bool requireMaster)
            {
                EventStreamId = eventStreamId;
                ExpectedVersion = expectedVersion;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class TransactionStartCompleted
        {
            [Key(0)]
            public readonly long TransactionId;
            [Key(1)]
            public readonly OperationResult Result;
            [Key(2)]
            public readonly string Message;

            private TransactionStartCompleted() { }

            [SerializationConstructor]
            public TransactionStartCompleted(long transactionId, OperationResult result, string message)
            {
                TransactionId = transactionId;
                Result = result;
                Message = message;
            }
        }

        [MessagePackObject]
        public sealed class TransactionWrite
        {
            [Key(0)]
            public readonly long TransactionId;
            [Key(1)]
            public readonly NewEvent[] Events;
            [Key(2)]
            public readonly bool RequireMaster;

            private TransactionWrite() { }

            [SerializationConstructor]
            public TransactionWrite(long transactionId, NewEvent[] events, bool requireMaster)
            {
                TransactionId = transactionId;
                Events = events;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class TransactionWriteCompleted
        {
            [Key(0)]
            public readonly long TransactionId;
            [Key(1)]
            public readonly OperationResult Result;
            [Key(2)]
            public readonly string Message;

            private TransactionWriteCompleted() { }

            [SerializationConstructor]
            public TransactionWriteCompleted(long transactionId, OperationResult result, string message)
            {
                TransactionId = transactionId;
                Result = result;
                Message = message;
            }
        }

        [MessagePackObject]
        public sealed class TransactionCommit
        {
            [Key(0)]
            public readonly long TransactionId;
            [Key(1)]
            public readonly bool RequireMaster;

            private TransactionCommit() { }

            [SerializationConstructor]
            public TransactionCommit(long transactionId, bool requireMaster)
            {
                TransactionId = transactionId;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class TransactionCommitCompleted
        {
            [Key(0)]
            public readonly long TransactionId;
            [Key(1)]
            public readonly OperationResult Result;
            [Key(2)]
            public readonly string Message;
            [Key(3)]
            public readonly long FirstEventNumber;
            [Key(4)]
            public readonly long LastEventNumber;
            [Key(5)]
            public readonly long? PreparePosition;
            [Key(6)]
            public readonly long? CommitPosition;

            private TransactionCommitCompleted() { }

            [SerializationConstructor]
            public TransactionCommitCompleted(long transactionId, OperationResult result, string message, long firstEventNumber, long lastEventNumber, long? preparePosition, long? commitPosition)
            {
                TransactionId = transactionId;
                Result = result;
                Message = message;
                FirstEventNumber = firstEventNumber;
                LastEventNumber = lastEventNumber;
                PreparePosition = preparePosition;
                CommitPosition = commitPosition;
            }
        }

        [MessagePackObject]
        public sealed class ReadEvent
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly long EventNumber;
            [Key(2)]
            public readonly bool ResolveLinkTos;
            [Key(3)]
            public readonly bool RequireMaster;

            private ReadEvent() { }

            [SerializationConstructor]
            public ReadEvent(string eventStreamId, long eventNumber, bool resolveLinkTos, bool requireMaster)
            {
                EventStreamId = eventStreamId;
                EventNumber = eventNumber;
                ResolveLinkTos = resolveLinkTos;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class ReadEventCompleted
        {
            [Key(0)]
            public readonly ReadEventCompleted.ReadEventResult Result;
            [Key(1)]
            public readonly ResolvedIndexedEvent Event;
            [Key(2)]
            public readonly string Error;

            public enum ReadEventResult
            {

                Success = 0,

                NotFound = 1,

                NoStream = 2,

                StreamDeleted = 3,

                Error = 4,

                AccessDenied = 5
            }

            private ReadEventCompleted() { }

            [SerializationConstructor]
            public ReadEventCompleted(ReadEventCompleted.ReadEventResult result, ResolvedIndexedEvent @event, string error)
            {
                Result = result;
                Event = @event;
                Error = error;
            }
        }

        [MessagePackObject]
        public sealed class ReadStreamEvents
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly long FromEventNumber;
            [Key(2)]
            public readonly int MaxCount;
            [Key(3)]
            public readonly bool ResolveLinkTos;
            [Key(4)]
            public readonly bool RequireMaster;

            private ReadStreamEvents() { }

            [SerializationConstructor]
            public ReadStreamEvents(string eventStreamId, long fromEventNumber, int maxCount, bool resolveLinkTos, bool requireMaster)
            {
                EventStreamId = eventStreamId;
                FromEventNumber = fromEventNumber;
                MaxCount = maxCount;
                ResolveLinkTos = resolveLinkTos;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class ReadStreamEventsCompleted
        {
            [Key(0)]
            public readonly ResolvedIndexedEvent[] Events;
            [Key(1)]
            public readonly ReadStreamEventsCompleted.ReadStreamResult Result;
            [Key(2)]
            public readonly long NextEventNumber;
            [Key(3)]
            public readonly long LastEventNumber;
            [Key(4)]
            public readonly bool IsEndOfStream;
            [Key(5)]
            public readonly long LastCommitPosition;
            [Key(6)]
            public readonly string Error;

            public enum ReadStreamResult
            {

                Success = 0,

                NoStream = 1,

                StreamDeleted = 2,

                NotModified = 3,

                Error = 4,

                AccessDenied = 5
            }

            private ReadStreamEventsCompleted() { }

            [SerializationConstructor]
            public ReadStreamEventsCompleted(ResolvedIndexedEvent[] events, ReadStreamEventsCompleted.ReadStreamResult result, long nextEventNumber, long lastEventNumber, bool isEndOfStream, long lastCommitPosition, string error)
            {
                Events = events;
                Result = result;
                NextEventNumber = nextEventNumber;
                LastEventNumber = lastEventNumber;
                IsEndOfStream = isEndOfStream;
                LastCommitPosition = lastCommitPosition;
                Error = error;
            }
        }

        [MessagePackObject]
        public sealed class ReadAllEvents
        {
            [Key(0)]
            public readonly long CommitPosition;
            [Key(1)]
            public readonly long PreparePosition;
            [Key(2)]
            public readonly int MaxCount;
            [Key(3)]
            public readonly bool ResolveLinkTos;
            [Key(4)]
            public readonly bool RequireMaster;

            private ReadAllEvents() { }

            [SerializationConstructor]
            public ReadAllEvents(long commitPosition, long preparePosition, int maxCount, bool resolveLinkTos, bool requireMaster)
            {
                CommitPosition = commitPosition;
                PreparePosition = preparePosition;
                MaxCount = maxCount;
                ResolveLinkTos = resolveLinkTos;
                RequireMaster = requireMaster;
            }
        }

        [MessagePackObject]
        public sealed class ReadAllEventsCompleted
        {
            [Key(0)]
            public readonly long CommitPosition;
            [Key(1)]
            public readonly long PreparePosition;
            [Key(2)]
            public readonly ResolvedEvent[] Events;
            [Key(3)]
            public readonly long NextCommitPosition;
            [Key(4)]
            public readonly long NextPreparePosition;
            [Key(5)]
            public readonly ReadAllEventsCompleted.ReadAllResult Result;
            [Key(6)]
            public readonly string Error;

            public enum ReadAllResult
            {

                Success = 0,

                NotModified = 1,

                Error = 2,

                AccessDenied = 3
            }

            private ReadAllEventsCompleted() { }

            [SerializationConstructor]
            public ReadAllEventsCompleted(long commitPosition, long preparePosition, ResolvedEvent[] events, long nextCommitPosition, long nextPreparePosition, ReadAllEventsCompleted.ReadAllResult result, string error)
            {
                CommitPosition = commitPosition;
                PreparePosition = preparePosition;
                Events = events;
                NextCommitPosition = nextCommitPosition;
                NextPreparePosition = nextPreparePosition;
                Result = result;
                Error = error;
            }
        }

        [MessagePackObject]
        public sealed class CreatePersistentSubscription
        {
            [Key(0)]
            public readonly string SubscriptionGroupName;
            [Key(1)]
            public readonly string EventStreamId;
            [Key(2)]
            public readonly bool ResolveLinkTos;
            [Key(3)]
            public readonly long StartFrom;
            [Key(4)]
            public readonly int MessageTimeoutMilliseconds;
            [Key(5)]
            public readonly bool RecordStatistics;
            [Key(6)]
            public readonly int LiveBufferSize;
            [Key(7)]
            public readonly int ReadBatchSize;
            [Key(8)]
            public readonly int BufferSize;
            [Key(9)]
            public readonly int MaxRetryCount;
            [Key(10)]
            public readonly bool PreferRoundRobin;
            [Key(11)]
            public readonly int CheckpointAfterTime;
            [Key(12)]
            public readonly int CheckpointMaxCount;
            [Key(13)]
            public readonly int CheckpointMinCount;
            [Key(14)]
            public readonly int SubscriberMaxCount;
            [Key(15)]
            public readonly string NamedConsumerStrategy;

            private CreatePersistentSubscription() { }

            [SerializationConstructor]
            public CreatePersistentSubscription(string subscriptionGroupName, string eventStreamId, bool resolveLinkTos, long startFrom, int messageTimeoutMilliseconds, bool recordStatistics, int liveBufferSize, int readBatchSize, int bufferSize, int maxRetryCount, bool preferRoundRobin, int checkpointAfterTime, int checkpointMaxCount, int checkpointMinCount, int subscriberMaxCount, string namedConsumerStrategy)
            {
                SubscriptionGroupName = subscriptionGroupName;
                EventStreamId = eventStreamId;
                ResolveLinkTos = resolveLinkTos;
                StartFrom = startFrom;
                MessageTimeoutMilliseconds = messageTimeoutMilliseconds;
                RecordStatistics = recordStatistics;
                LiveBufferSize = liveBufferSize;
                ReadBatchSize = readBatchSize;
                BufferSize = bufferSize;
                MaxRetryCount = maxRetryCount;
                PreferRoundRobin = preferRoundRobin;
                CheckpointAfterTime = checkpointAfterTime;
                CheckpointMaxCount = checkpointMaxCount;
                CheckpointMinCount = checkpointMinCount;
                SubscriberMaxCount = subscriberMaxCount;
                NamedConsumerStrategy = namedConsumerStrategy;
            }
        }

        [MessagePackObject]
        public sealed class DeletePersistentSubscription
        {
            [Key(0)]
            public readonly string SubscriptionGroupName;
            [Key(1)]
            public readonly string EventStreamId;

            private DeletePersistentSubscription() { }

            [SerializationConstructor]
            public DeletePersistentSubscription(string subscriptionGroupName, string eventStreamId)
            {
                SubscriptionGroupName = subscriptionGroupName;
                EventStreamId = eventStreamId;
            }
        }

        [MessagePackObject]
        public sealed class UpdatePersistentSubscription
        {
            [Key(0)]
            public readonly string SubscriptionGroupName;
            [Key(1)]
            public readonly string EventStreamId;
            [Key(2)]
            public readonly bool ResolveLinkTos;
            [Key(3)]
            public readonly long StartFrom;
            [Key(4)]
            public readonly int MessageTimeoutMilliseconds;
            [Key(5)]
            public readonly bool RecordStatistics;
            [Key(6)]
            public readonly int LiveBufferSize;
            [Key(7)]
            public readonly int ReadBatchSize;
            [Key(8)]
            public readonly int BufferSize;
            [Key(9)]
            public readonly int MaxRetryCount;
            [Key(10)]
            public readonly bool PreferRoundRobin;
            [Key(11)]
            public readonly int CheckpointAfterTime;
            [Key(12)]
            public readonly int CheckpointMaxCount;
            [Key(13)]
            public readonly int CheckpointMinCount;
            [Key(14)]
            public readonly int SubscriberMaxCount;
            [Key(15)]
            public readonly string NamedConsumerStrategy;

            private UpdatePersistentSubscription() { }

            [SerializationConstructor]
            public UpdatePersistentSubscription(string subscriptionGroupName, string eventStreamId, bool resolveLinkTos, long startFrom, int messageTimeoutMilliseconds, bool recordStatistics, int liveBufferSize, int readBatchSize, int bufferSize, int maxRetryCount, bool preferRoundRobin, int checkpointAfterTime, int checkpointMaxCount, int checkpointMinCount, int subscriberMaxCount, string namedConsumerStrategy)
            {
                SubscriptionGroupName = subscriptionGroupName;
                EventStreamId = eventStreamId;
                ResolveLinkTos = resolveLinkTos;
                StartFrom = startFrom;
                MessageTimeoutMilliseconds = messageTimeoutMilliseconds;
                RecordStatistics = recordStatistics;
                LiveBufferSize = liveBufferSize;
                ReadBatchSize = readBatchSize;
                BufferSize = bufferSize;
                MaxRetryCount = maxRetryCount;
                PreferRoundRobin = preferRoundRobin;
                CheckpointAfterTime = checkpointAfterTime;
                CheckpointMaxCount = checkpointMaxCount;
                CheckpointMinCount = checkpointMinCount;
                SubscriberMaxCount = subscriberMaxCount;
                NamedConsumerStrategy = namedConsumerStrategy;
            }
        }

        [MessagePackObject]
        public sealed class UpdatePersistentSubscriptionCompleted
        {
            [Key(0)]
            public readonly UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult Result;
            [Key(1)]
            public readonly string Reason;

            public enum UpdatePersistentSubscriptionResult
            {

                Success = 0,

                DoesNotExist = 1,

                Fail = 2,

                AccessDenied = 3
            }

            private UpdatePersistentSubscriptionCompleted() { }

            [SerializationConstructor]
            public UpdatePersistentSubscriptionCompleted(UpdatePersistentSubscriptionCompleted.UpdatePersistentSubscriptionResult result, string reason)
            {
                Result = result;
                Reason = reason;
            }
        }

        [MessagePackObject]
        public sealed class CreatePersistentSubscriptionCompleted
        {
            [Key(0)]
            public readonly CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult Result;
            [Key(1)]
            public readonly string Reason;

            public enum CreatePersistentSubscriptionResult
            {

                Success = 0,

                AlreadyExists = 1,

                Fail = 2,

                AccessDenied = 3
            }

            private CreatePersistentSubscriptionCompleted() { }

            [SerializationConstructor]
            public CreatePersistentSubscriptionCompleted(CreatePersistentSubscriptionCompleted.CreatePersistentSubscriptionResult result, string reason)
            {
                Result = result;
                Reason = reason;
            }
        }

        [MessagePackObject]
        public sealed class DeletePersistentSubscriptionCompleted
        {
            [Key(0)]
            public readonly DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult Result;
            [Key(1)]
            public readonly string Reason;

            public enum DeletePersistentSubscriptionResult
            {

                Success = 0,

                DoesNotExist = 1,

                Fail = 2,

                AccessDenied = 3
            }

            private DeletePersistentSubscriptionCompleted() { }

            [SerializationConstructor]
            public DeletePersistentSubscriptionCompleted(DeletePersistentSubscriptionCompleted.DeletePersistentSubscriptionResult result, string reason)
            {
                Result = result;
                Reason = reason;
            }
        }

        [MessagePackObject]
        public sealed class ConnectToPersistentSubscription
        {
            [Key(0)]
            public readonly string SubscriptionId;
            [Key(1)]
            public readonly string EventStreamId;
            [Key(2)]
            public readonly int AllowedInFlightMessages;

            private ConnectToPersistentSubscription() { }

            [SerializationConstructor]
            public ConnectToPersistentSubscription(string subscriptionId, string eventStreamId, int allowedInFlightMessages)
            {
                SubscriptionId = subscriptionId;
                EventStreamId = eventStreamId;
                AllowedInFlightMessages = allowedInFlightMessages;
            }
        }

        [MessagePackObject]
        public sealed class PersistentSubscriptionAckEvents
        {
            [Key(0)]
            public readonly string SubscriptionId;
            [Key(1)]
            public readonly byte[][] ProcessedEventIds;

            private PersistentSubscriptionAckEvents() { }

            [SerializationConstructor]
            public PersistentSubscriptionAckEvents(string subscriptionId, byte[][] processedEventIds)
            {
                SubscriptionId = subscriptionId;
                ProcessedEventIds = processedEventIds;
            }
        }

        [MessagePackObject]
        public sealed class PersistentSubscriptionNakEvents
        {
            [Key(0)]
            public readonly string SubscriptionId;
            [Key(1)]
            public readonly byte[][] ProcessedEventIds;
            [Key(2)]
            public readonly string Message;
            [Key(3)]
            public readonly PersistentSubscriptionNakEvents.NakAction Action;

            public enum NakAction
            {

                Unknown = 0,

                Park = 1,

                Retry = 2,

                Skip = 3,

                Stop = 4
            }

            private PersistentSubscriptionNakEvents() { }

            [SerializationConstructor]
            public PersistentSubscriptionNakEvents(string subscriptionId, byte[][] processedEventIds, string message, PersistentSubscriptionNakEvents.NakAction action)
            {
                SubscriptionId = subscriptionId;
                ProcessedEventIds = processedEventIds;
                Message = message;
                Action = action;
            }
        }

        [MessagePackObject]
        public sealed class PersistentSubscriptionConfirmation
        {
            [Key(0)]
            public readonly long LastCommitPosition;
            [Key(1)]
            public readonly string SubscriptionId;
            [Key(2)]
            public readonly long? LastEventNumber;

            private PersistentSubscriptionConfirmation() { }

            [SerializationConstructor]
            public PersistentSubscriptionConfirmation(long lastCommitPosition, string subscriptionId, long? lastEventNumber)
            {
                LastCommitPosition = lastCommitPosition;
                SubscriptionId = subscriptionId;
                LastEventNumber = lastEventNumber;
            }
        }

        [MessagePackObject]
        public sealed class PersistentSubscriptionStreamEventAppeared
        {
            [Key(0)]
            public readonly ResolvedIndexedEvent Event;
            [Key(1)]
            public readonly int? RetryCount;

            private PersistentSubscriptionStreamEventAppeared() { }

            [SerializationConstructor]
            public PersistentSubscriptionStreamEventAppeared(ResolvedIndexedEvent @event, int? retryCount)
            {
                Event = @event;
                RetryCount = retryCount;
            }
        }

        [MessagePackObject]
        public sealed class SubscribeToStream
        {
            [Key(0)]
            public readonly string EventStreamId;
            [Key(1)]
            public readonly bool ResolveLinkTos;

            private SubscribeToStream() { }

            [SerializationConstructor]
            public SubscribeToStream(string eventStreamId, bool resolveLinkTos)
            {
                EventStreamId = eventStreamId;
                ResolveLinkTos = resolveLinkTos;
            }
        }

        [MessagePackObject]
        public sealed class SubscriptionConfirmation
        {
            [Key(0)]
            public readonly long LastCommitPosition;
            [Key(1)]
            public readonly long? LastEventNumber;

            private SubscriptionConfirmation() { }

            [SerializationConstructor]
            public SubscriptionConfirmation(long lastCommitPosition, long? lastEventNumber)
            {
                LastCommitPosition = lastCommitPosition;
                LastEventNumber = lastEventNumber;
            }
        }

        [MessagePackObject]
        public sealed class StreamEventAppeared
        {
            [Key(0)]
            public readonly ResolvedEvent Event;

            private StreamEventAppeared() { }

            [SerializationConstructor]
            public StreamEventAppeared(ResolvedEvent @event)
            {
                Event = @event;
            }
        }

        [MessagePackObject]
        public sealed class UnsubscribeFromStream
        {
            public UnsubscribeFromStream()
            {
            }
        }

        [MessagePackObject]
        public sealed class SubscriptionDropped
        {
            [Key(0)]
            public readonly SubscriptionDropped.SubscriptionDropReason Reason;

            public enum SubscriptionDropReason
            {
                Unsubscribed = 0,

                AccessDenied = 1,

                NotFound = 2,

                PersistentSubscriptionDeleted = 3,

                SubscriberMaxCountReached = 4
            }

            private SubscriptionDropped() { }

            [SerializationConstructor]
            public SubscriptionDropped(SubscriptionDropped.SubscriptionDropReason reason)
            {
                Reason = reason;
            }
        }

        [MessagePackObject]
        public sealed partial class NotHandled
        {
            [Key(0)]
            public readonly NotHandled.NotHandledReason Reason;
            [Key(1)]
            public readonly byte[] AdditionalInfo;

            [MessagePackObject]
            public sealed partial class MasterInfo
            {
                [Key(0)]
                public readonly string ExternalTcpAddress;
                [Key(1)]
                public readonly int ExternalTcpPort;
                [Key(2)]
                public readonly string ExternalHttpAddress;
                [Key(3)]
                public readonly int ExternalHttpPort;
                [Key(4)]
                public readonly string ExternalSecureTcpAddress;
                [Key(5)]
                public readonly int? ExternalSecureTcpPort;

                [IgnoreMember]
                public IPEndPoint ExternalTcpEndPoint { get { return new IPEndPoint(IPAddress.Parse(ExternalTcpAddress), ExternalTcpPort); } }

                [IgnoreMember]
                public IPEndPoint ExternalSecureTcpEndPoint
                {
                    get
                    {
                        return ExternalSecureTcpAddress == null || ExternalSecureTcpPort == null
                                ? null
                                : new IPEndPoint(IPAddress.Parse(ExternalSecureTcpAddress), ExternalSecureTcpPort.Value);
                    }
                }

                [IgnoreMember]
                public IPEndPoint ExternalHttpEndPoint { get { return new IPEndPoint(IPAddress.Parse(ExternalHttpAddress), ExternalHttpPort); } }

                private MasterInfo() { }

                [SerializationConstructor]
                public MasterInfo(string externalTcpAddress, int externalTcpPort, string externalHttpAddress, int externalHttpPort, string externalSecureTcpAddress, int? externalSecureTcpPort)
                {
                    ExternalTcpAddress = externalTcpAddress;
                    ExternalTcpPort = externalTcpPort;
                    ExternalHttpAddress = externalHttpAddress;
                    ExternalHttpPort = externalHttpPort;
                    ExternalSecureTcpAddress = externalSecureTcpAddress;
                    ExternalSecureTcpPort = externalSecureTcpPort;
                }

                public MasterInfo(IPEndPoint externalTcpEndPoint, IPEndPoint externalSecureTcpEndPoint, IPEndPoint externalHttpEndPoint)
                {
                    ExternalTcpAddress = externalTcpEndPoint.Address.ToString();
                    ExternalTcpPort = externalTcpEndPoint.Port;
                    ExternalSecureTcpAddress = externalSecureTcpEndPoint == null ? null : externalSecureTcpEndPoint.Address.ToString();
                    ExternalSecureTcpPort = externalSecureTcpEndPoint == null ? (int?)null : externalSecureTcpEndPoint.Port;
                    ExternalHttpAddress = externalHttpEndPoint.Address.ToString();
                    ExternalHttpPort = externalHttpEndPoint.Port;
                }
            }

            public enum NotHandledReason
            {

                NotReady = 0,

                TooBusy = 1,

                NotMaster = 2
            }

            private NotHandled() { }

            [SerializationConstructor]
            public NotHandled(NotHandled.NotHandledReason reason, byte[] additionalInfo)
            {
                Reason = reason;
                AdditionalInfo = additionalInfo;
            }
        }

        [MessagePackObject]
        public sealed class ScavengeDatabase
        {
            public ScavengeDatabase()
            {
            }
        }

        [MessagePackObject]
        public sealed class ScavengeDatabaseResponse
        {
            [Key(0)]
            public readonly ScavengeDatabaseResponse.ScavengeResult Result;
            [Key(1)]
            public readonly string ScavengeId;

            public enum ScavengeResult
            {

                Started = 0,

                InProgress = 1,

                Unauthorized = 2
            }

            private ScavengeDatabaseResponse() { }

            [SerializationConstructor]
            public ScavengeDatabaseResponse(ScavengeDatabaseResponse.ScavengeResult result, string scavengeId)
            {
                Result = result;
                ScavengeId = scavengeId;
            }
        }

        [MessagePackObject]
        public sealed class IdentifyClient
        {
            [Key(0)]
            public readonly int Version;
            [Key(1)]
            public readonly string ConnectionName;

            private IdentifyClient() { }

            [SerializationConstructor]
            public IdentifyClient(int version, string connectionName)
            {
                Version = version;
                ConnectionName = connectionName;
            }
        }

        [MessagePackObject]
        public sealed class ClientIdentified
        {
            public ClientIdentified()
            {
            }
        }
    }
}