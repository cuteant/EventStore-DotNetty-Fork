using System.Collections.Generic;

namespace EventStore.ClientAPI
{
    public interface IEventMetadata
    {
        string ClrEventType { get; set; }

        Dictionary<string, object> Context { get; }
    }
}
