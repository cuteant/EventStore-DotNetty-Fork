using System;
using System.Net;
using EventStore.Transport.Tcp.Messages;

namespace EventStore.Core.Messages
{
    public static class TcpClientMessageDtoExtensions
    {

        public static TcpClientMessageDto.ResolvedIndexedEvent CreateResolvedIndexedEvent(Data.EventRecord eventRecord, Data.EventRecord linkRecord)
        {
            return new TcpClientMessageDto.ResolvedIndexedEvent(eventRecord?.ToEventRecord(), linkRecord?.ToEventRecord());
        }
        //public partial class ResolvedIndexedEvent
        //{
        //    public ResolvedIndexedEvent(Data.EventRecord eventRecord, Data.EventRecord linkRecord)
        //        : this(eventRecord != null ? new EventRecord(eventRecord) : null,
        //               linkRecord != null ? new EventRecord(linkRecord) : null)
        //    {
        //    }
        //}

        public static TcpClientMessageDto.ResolvedEvent ToResolvedEvent(this Data.ResolvedEvent pair)
        {
            return new TcpClientMessageDto.ResolvedEvent(pair.Event != null ? pair.Event.ToEventRecord() : null,
                       pair.Link != null ? pair.Link.ToEventRecord() : null,
                       pair.OriginalPosition.Value.CommitPosition,
                       pair.OriginalPosition.Value.PreparePosition);
        }
        //public partial class ResolvedEvent
        //{
        //    public ResolvedEvent(Data.ResolvedEvent pair)
        //        : this(pair.Event != null ? new EventRecord(pair.Event) : null,
        //               pair.Link != null ? new EventRecord(pair.Link) : null,
        //               pair.OriginalPosition.Value.CommitPosition,
        //               pair.OriginalPosition.Value.PreparePosition)
        //    {
        //    }
        //}

        public static TcpClientMessageDto.EventRecord ToEventRecord(this Data.EventRecord eventRecord)
        {
            var isJson = eventRecord.IsJson;
            return new TcpClientMessageDto.EventRecord(
                eventStreamId: eventRecord.EventStreamId,
                eventNumber: eventRecord.EventNumber,
                eventId: eventRecord.EventId.ToByteArray(),
                eventType: eventRecord.EventType,
                dataContentType: isJson ? 1 : 0,
                metadataContentType: isJson ? 1 : 0,
                data: eventRecord.Data,
                metadata: eventRecord.Metadata,
                created: eventRecord.TimeStamp.ToBinary(),
                createdEpoch: (long)(eventRecord.TimeStamp - new DateTime(1970, 1, 1)).TotalMilliseconds
            );
        }
        //public partial class EventRecord
        //{
        //    public EventRecord(Data.EventRecord eventRecord)
        //    {
        //        EventStreamId = eventRecord.EventStreamId;
        //        EventNumber = eventRecord.EventNumber;
        //        EventId = eventRecord.EventId.ToByteArray();
        //        EventType = eventRecord.EventType;
        //        Data = eventRecord.Data;
        //        Created = eventRecord.TimeStamp.ToBinary();
        //        Metadata = eventRecord.Metadata;
        //        var isJson = eventRecord.IsJson;
        //        DataContentType = isJson ? 1 : 0;
        //        MetadataContentType = isJson ? 1 : 0;
        //        CreatedEpoch = (long) (eventRecord.TimeStamp - new DateTime(1970, 1, 1)).TotalMilliseconds;
        //    }
        public static TcpClientMessageDto.EventRecord ToEventRecord(this Data.EventRecord eventRecord, long eventNumber)
        {
            var isJson = eventRecord.IsJson;
            return new TcpClientMessageDto.EventRecord(
                eventStreamId: eventRecord.EventStreamId,
                eventNumber: eventNumber,
                eventId: eventRecord.EventId.ToByteArray(),
                eventType: eventRecord.EventType,
                dataContentType: isJson ? 1 : 0,
                metadataContentType: isJson ? 1 : 0,
                data: eventRecord.Data,
                metadata: eventRecord.Metadata,
                created: eventRecord.TimeStamp.ToBinary(),
                createdEpoch: (long)(eventRecord.TimeStamp - new DateTime(1970, 1, 1)).TotalMilliseconds
            );
        }

        //    public EventRecord(Data.EventRecord eventRecord, long eventNumber)
        //    {
        //        EventStreamId = eventRecord.EventStreamId;
        //        EventNumber = eventNumber;
        //        EventId = eventRecord.EventId.ToByteArray();
        //        EventType = eventRecord.EventType;
        //        Data = eventRecord.Data;
        //        Created = eventRecord.TimeStamp.ToBinary();
        //        Metadata = eventRecord.Metadata;
        //        var isJson = eventRecord.IsJson;
        //        DataContentType = isJson ? 1 : 0;
        //        MetadataContentType = isJson ? 1 : 0;
        //        CreatedEpoch = (long) (eventRecord.TimeStamp - new DateTime(1970, 1, 1)).TotalMilliseconds;
        //    }
        //}

        //public partial class NotHandled
        //{
        //    public partial class MasterInfo
        //    {
        //        public MasterInfo(IPEndPoint externalTcpEndPoint, IPEndPoint externalSecureTcpEndPoint, IPEndPoint externalHttpEndPoint)
        //        {
        //            ExternalTcpAddress = externalTcpEndPoint.Address.ToString();
        //            ExternalTcpPort = externalTcpEndPoint.Port;
        //            ExternalSecureTcpAddress = externalSecureTcpEndPoint == null ? null : externalSecureTcpEndPoint.Address.ToString();
        //            ExternalSecureTcpPort = externalSecureTcpEndPoint == null ? (int?)null : externalSecureTcpEndPoint.Port;
        //            ExternalHttpAddress = externalHttpEndPoint.Address.ToString();
        //            ExternalHttpPort = externalHttpEndPoint.Port;
        //        }
        //    }
        //}
    }
}
