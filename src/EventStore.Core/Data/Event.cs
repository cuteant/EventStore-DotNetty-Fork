﻿using System;
using EventStore.Common.Utils;
using EventStore.Core.TransactionLog.Chunks;
namespace EventStore.Core.Data
{
    public class Event
    {
        public readonly Guid EventId;
        public readonly string EventType;
        public readonly bool IsJson;

        public readonly byte[] Data;
        public readonly byte[] Metadata;

        public Event(Guid eventId, string eventType, bool isJson, string data, string metadata)
            : this(
                eventId, eventType, isJson, Helper.UTF8NoBom.GetBytes(data),
                metadata != null ? Helper.UTF8NoBom.GetBytes(metadata) : null)
        {
        }

        public Event(Guid eventId, string eventType, bool isJson, byte[] data, byte[] metadata)
        {
            if (Guid.Empty == eventId)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Empty_eventId_provided);
            if (string.IsNullOrEmpty(eventType))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Empty_eventType_provided);

            EventId = eventId;
            EventType = eventType;
            IsJson = isJson;
            Data = data ?? Empty.ByteArray;
            Metadata = metadata ?? Empty.ByteArray;

            var size = Data == null ? 0 : Data.Length;
            size += Metadata == null ? 0 : Metadata.Length;
            size += eventType.Length * 2;

            if( size > TFConsts.MaxLogRecordSize - 10000)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Record_is_too_big, ExceptionArgument.data);
        }
    }
}