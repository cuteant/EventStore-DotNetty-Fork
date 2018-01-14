using System;
using EventStore.Core.Messages;

namespace EventStore.ClientAPI.SystemData
{
    internal class StatusCode
    {
        public static SliceReadStatus Convert(TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult code)
        {
            switch (code)
            {
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.Success:
                    return SliceReadStatus.Success;
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.NoStream:
                    return SliceReadStatus.StreamNotFound;
                case TcpClientMessageDto.ReadStreamEventsCompleted.ReadStreamResult.StreamDeleted:
                    return SliceReadStatus.StreamDeleted;
                default:
                    throw new ArgumentOutOfRangeException("code");
            }
        }
    }
}
