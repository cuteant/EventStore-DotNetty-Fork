using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CuteAnt.Buffers;
using CuteAnt.IO;
using EventStore.Common.Utils;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Http.Codecs
{
    public class XmlCodec : ICodec
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<XmlCodec>();

        public string ContentType { get { return EventStore.Transport.Http.ContentType.Xml; } }
        public Encoding Encoding { get { return Helper.UTF8NoBom; } }
        public bool HasEventIds { get { return false; } }
        public bool HasEventTypes { get { return false; } }

        public bool CanParse(MediaType format)
        {
            return format != null && format.Matches(ContentType, Encoding);
        }

        public bool SuitableForResponse(MediaType component)
        {
            return component.Type == "*"
                   || (string.Equals(component.Type, "text", StringComparison.OrdinalIgnoreCase)
                       && (component.Subtype == "*"
                           || string.Equals(component.Subtype, "xml", StringComparison.OrdinalIgnoreCase)));
        }

        public T From<T>(string text)
        {
            if (string.IsNullOrEmpty(text)) { return default(T); }

            try
            {
                using (var reader = new StringReader(text))
                {
                    return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                Log.IsNotAValidSerialized<T>(text, e);
                return default(T);
            }
        }

        public string To<T>(T value)
        {
            if (value == null) { return null; }

            if ((object)value == Empty.Result) { return Empty.Xml; }

            try
            {
                using (var memory = MemoryStreamManager.GetStream())
                {
                    using (var writer = new XmlTextWriter(memory, Helper.UTF8NoBom))
                    {
                        if (value is IXmlSerializable serializable)
                        {
                            writer.WriteStartDocument();
                            serializable.WriteXml(writer);
                            writer.WriteEndDocument();
                        }
                        else
                        {
                            new XmlSerializer(typeof(T)).Serialize(writer, value);
                        }

                        writer.Flush();
                        return Helper.UTF8NoBom.GetString(memory.GetBuffer(), 0, (int)memory.Length);
                    }
                }
            }
            catch (Exception exc)
            {
                Log.ErrorSerializingObjectOfType<T>(exc);
                return null;
            }
        }
    }
}