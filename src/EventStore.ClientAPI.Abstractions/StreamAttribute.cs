using System;
using System.Collections.Generic;

namespace EventStore.ClientAPI
{
  /// <summary>ExpectedVersion: any, noStream, emptyStream, streamExists</summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
  public class StreamAttribute : Attribute
  {
    public readonly string StreamId;

    public readonly string EventType;

    public readonly long ExpectedVersion;

    private static readonly IDictionary<string, long> s_expectedVersionMap;

    static StreamAttribute()
    {
      s_expectedVersionMap = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
      {
        { "any", EventStore.ClientAPI.ExpectedVersion.Any},
        { "noStream", EventStore.ClientAPI.ExpectedVersion.NoStream},
        { "emptyStream", EventStore.ClientAPI.ExpectedVersion.EmptyStream},
        { "streamExists", EventStore.ClientAPI.ExpectedVersion.StreamExists}
      };
    }

    public StreamAttribute(string stream, string eventType = null, string expectedVersion = null)
    {
      if (string.IsNullOrEmpty(stream)) { throw new ArgumentNullException(nameof(stream)); }

      StreamId = stream;
      EventType = eventType;
      if (!string.IsNullOrEmpty(expectedVersion) && s_expectedVersionMap.TryGetValue(expectedVersion, out long version))
      {
        ExpectedVersion = version;
      }
      else
      {
        ExpectedVersion = EventStore.ClientAPI.ExpectedVersion.Any;
      }
    }
  }
}