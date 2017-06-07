using System;
using System.Net;
using EventStore.ClientAPI.Internal;

namespace EventStore.ClientAPI.Messages
{
  internal static partial class ClientMessage
  {
    public partial class NotHandled
    {
      public partial class MasterInfo
      {
        public IPEndPoint ExternalTcpEndPoint { get { return new IPEndPoint(IPAddress.Parse(ExternalTcpAddress), ExternalTcpPort); } }

        public IPEndPoint ExternalSecureTcpEndPoint
        {
          get
          {
            return ExternalSecureTcpAddress == null || ExternalSecureTcpPort == null
                    ? null
                    : new IPEndPoint(IPAddress.Parse(ExternalSecureTcpAddress), ExternalSecureTcpPort.Value);
          }
        }

        public IPEndPoint ExternalHttpEndPoint { get { return new IPEndPoint(IPAddress.Parse(ExternalHttpAddress), ExternalHttpPort); } }
      }
    }

    internal static RecordedEvent ToRecordedEvent(this ClientMessage.EventRecord systemRecord)
    {
      return new RecordedEvent(
        systemRecord.EventStreamId,
        new Guid(systemRecord.EventId),
        systemRecord.EventNumber,
        systemRecord.EventType,
        systemRecord.Created,
        systemRecord.CreatedEpoch,
        systemRecord.Data ?? Empty.ByteArray,
        systemRecord.Metadata ?? Empty.ByteArray,
        systemRecord.DataContentType == 1);
    }


    internal static ClientAPI.ResolvedEvent ToResolvedEvent(this ClientMessage.ResolvedEvent evnt)
    {
      return new ClientAPI.ResolvedEvent(
          evnt.Event?.ToRecordedEvent(),
          evnt.Link?.ToRecordedEvent(),
          new Position(evnt.CommitPosition, evnt.PreparePosition));
    }

    internal static ClientAPI.ResolvedEvent ToResolvedEvent(this ClientMessage.ResolvedIndexedEvent evnt)
    {
      return new ClientAPI.ResolvedEvent(evnt.Event?.ToRecordedEvent(), evnt.Link?.ToRecordedEvent(), null);
    }

    internal static ClientAPI.ResolvedEvent? ToResolvedEvent(this ClientMessage.ResolvedIndexedEvent evnt, EventReadStatus readStatus)
    {
      return readStatus == EventReadStatus.Success
            ? new ClientAPI.ResolvedEvent(evnt.Event?.ToRecordedEvent(), evnt.Link?.ToRecordedEvent(), null)
            : default(ClientAPI.ResolvedEvent?);
    }

    internal static ClientAPI.ResolvedEvent[] ToResolvedEvents(this ClientMessage.ResolvedEvent[] events)
    {
      if (events == null || events.Length == 0)
      {
        return Empty.ResolvedEvents;
      }
      else
      {
        var result = new ClientAPI.ResolvedEvent[events.Length];
        for (int i = 0; i < result.Length; ++i)
        {
          result[i] = events[i].ToResolvedEvent();
        }
        return result;
      }
    }

    internal static ClientAPI.ResolvedEvent[] ToResolvedEvents(this ClientMessage.ResolvedIndexedEvent[] events)
    {
      if (events == null || events.Length == 0)
      {
        return Empty.ResolvedEvents;
      }
      else
      {
        var result = new ClientAPI.ResolvedEvent[events.Length];
        for (int i = 0; i < result.Length; ++i)
        {
          result[i] = events[i].ToResolvedEvent();
        }
        return result;
      }
    }
  }
}
