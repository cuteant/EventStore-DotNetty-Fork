using System.Collections.Generic;
using CuteAnt.Extensions.Serialization;
using Newtonsoft.Json;

namespace EventStore.ClientAPI.Internal
{
  public sealed class EventMetadata
  {
    [JsonProperty("type")]
    public string EventType { get; set; }

    [JsonProperty("token")]
    public SerializingToken Token { get; set; }

    [JsonProperty("context")]
    public Dictionary<string, object> Context { get; set; }
  }
}
