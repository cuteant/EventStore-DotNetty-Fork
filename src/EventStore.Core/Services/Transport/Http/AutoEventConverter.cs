using System;
using System.Linq;
using System.Xml.Linq;
using EventStore.Common.Utils;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.TransactionLog.LogRecords;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace EventStore.Core.Services.Transport.Http
{
    public static class AutoEventConverter
    {
        private static readonly ILogger Log = TraceLogger.GetLogger(typeof(AutoEventConverter));

        public static object SmartFormat(in ResolvedEvent evnt, ICodec targetCodec)
        {
            var dto = CreateDataDto(evnt);

            switch (targetCodec.ContentType)
            {
                case ContentType.Raw:
                    return evnt.Event.Data;
                case ContentType.Xml:
                case ContentType.ApplicationXml:
                    {
                        var serializeObject = JsonConvertX.SerializeObject(dto.data);
                        var deserializeXmlNode = JsonConvertX.DeserializeXmlNode(serializeObject, "data");
                        return deserializeXmlNode.InnerXml;
                    }
                case ContentType.Json:
                    return targetCodec.To(dto.data);


                case ContentType.Atom:
                case ContentType.EventXml:
                    {
                        var serializeObject = JsonConvertX.SerializeObject(dto);
                        var deserializeXmlNode = JsonConvertX.DeserializeXmlNode(serializeObject, "event");
                        return deserializeXmlNode.InnerXml;
                    }

                case ContentType.EventJson:
                    return targetCodec.To(dto);


                default:
                    throw new NotSupportedException();
            }
        }

        public static HttpClientMessageDto.ReadEventCompletedText CreateDataDto(in ResolvedEvent evnt)
        {
            var dto = new HttpClientMessageDto.ReadEventCompletedText(evnt);
            if (evnt.Event.Flags.HasFlag(PrepareFlags.IsJson))
            {
                var deserializedData = Codec.Json.From<object>((string)dto.data);
                var deserializedMetadata = Codec.Json.From<object>((string)dto.metadata);

                if (deserializedData != null)
                    dto.data = deserializedData;
                if (deserializedMetadata != null)
                    dto.metadata = deserializedMetadata;
            }
            return dto;
        }

        //TODO GFY THERE IS WAY TOO MUCH COPYING/SERIALIZING/DESERIALIZING HERE!
        public static Event[] SmartParse(byte[] request, ICodec sourceCodec, Guid includedId, string includedType = null)
        {
            switch (sourceCodec.ContentType)
            {
                case ContentType.Raw:
                    return LoadRaw(request, includedId, includedType);
                case ContentType.Json:
                    return LoadRaw(sourceCodec.Encoding.GetString(request), true, includedId, includedType);
                case ContentType.EventJson:
                case ContentType.EventsJson:
                case ContentType.AtomJson:
                    var writeEvents = LoadFromJson(sourceCodec.Encoding.GetString(request));
                    if (writeEvents.IsEmpty()) return null;
                    return Parse(writeEvents);

                case ContentType.ApplicationXml:
                case ContentType.Xml:
                    return LoadRaw(sourceCodec.Encoding.GetString(request), false, includedId, includedType);
                case ContentType.EventXml:
                case ContentType.EventsXml:
                case ContentType.Atom:
                    var writeEvents2 = LoadFromXml(sourceCodec.Encoding.GetString(request));
                    if (writeEvents2.IsEmpty()) return null;
                    return Parse(writeEvents2);
                default:
                    return null;
            }
        }

        private static Event[] LoadRaw(byte[] data, Guid includedId, string includedType)
        {
            var ret = new Event[1];
            ret[0] = new Event(includedId, includedType, false, data, null);
            return ret;
        }

        private static Event[] LoadRaw(string data, bool isJson, Guid includedId, string includedType)
        {
            var ret = new Event[1];
            ret[0] = new Event(includedId, includedType, isJson, data, null);
            return ret;
        }

        private static HttpClientMessageDto.ClientEventDynamic[] LoadFromJson(string json)
        {
            return Codec.Json.From<HttpClientMessageDto.ClientEventDynamic[]>(json);
        }

        private static HttpClientMessageDto.ClientEventDynamic[] LoadFromXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);

                XNamespace jsonNsValue = "http://james.newtonking.com/projects/json";
                XName jsonNsName = XNamespace.Xmlns + "json";

                doc.Root.SetAttributeValue(jsonNsName, jsonNsValue);

                var events = doc.Root.Elements()/*.ToArray()*/;
                foreach (var @event in events)
                {
                    @event.Name = "events";
                    @event.SetAttributeValue(jsonNsValue + "Array", "true");
                }
                //doc.Root.ReplaceNodes(events);
                //                foreach (var element in doc.Root.Descendants("data").Concat(doc.Root.Descendants("metadata")))
                //                {
                //                    element.RemoveAttributes();
                //                }

                var json = JsonConvertX.SerializeXNode(doc.Root, Formatting.None, true);
                var root = JsonConvertX.DeserializeObject<HttpClientMessageDto.WriteEventsDynamic>(json);
                return root.events;
                //                var root = JsonConvertX.DeserializeObject<JObject>(json);
                //                var dynamicEvents = root.ToObject<HttpClientMessageDto.WriteEventsDynamic>();
                //                return dynamicEvents.events;
            }
            catch (Exception e)
            {
                if (Log.IsInformationLevelEnabled()) Log.LogInformation(e, "Failed to load xml. Invalid format");
                return null;
            }
        }

        private static Event[] Parse(HttpClientMessageDto.ClientEventDynamic[] dynamicEvents)
        {
            var events = new Event[dynamicEvents.Length];
            for (int i = 0, n = dynamicEvents.Length; i < n; ++i)
            {
                var textEvent = dynamicEvents[i];
                var data = AsBytes(textEvent.data, out bool dataIsJson);
                var metadata = AsBytes(textEvent.metadata, out bool metadataIsJson);

                events[i] = new Event(textEvent.eventId, textEvent.eventType, dataIsJson || metadataIsJson, data, metadata);
            }
            return events.ToArray();
        }

        private static byte[] AsBytes(object obj, out bool isJson)
        {
            if (obj is JObject || obj is JArray)
            {
                isJson = true;
                return Helper.UTF8NoBom.GetBytes(Codec.Json.To(obj));
            }

            isJson = false;
            return Helper.UTF8NoBom.GetBytes((obj as string) ?? string.Empty);
        }
    }
}
