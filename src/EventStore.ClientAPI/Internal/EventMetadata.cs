using System.Collections.Generic;
using Newtonsoft.Json;

namespace EventStore.ClientAPI.Internal
{
    public sealed class EventMetadata : IEventMetadata
    {
        [JsonProperty("clrEventType")]
        public string ClrEventType { get; set; }

        [JsonProperty("context")]
        public Dictionary<string, object> Context { get; set; }
    }
}
