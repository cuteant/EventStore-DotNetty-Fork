using System;

namespace EventStore.ClientAPI
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
  public class StreamMetadataAttribute : Attribute
  {
    /// <summary>The maximum number of events allowed in the stream.</summary>
    public long? MaxCount { get; set; }

    /// <summary>The maximum age of events allowed in the stream.</summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>The event number from which previous events can be scavenged.
    /// This is used to implement soft-deletion of streams.</summary>
    public long? TruncateBefore { get; set; }

    /// <summary>The amount of time for which the stream head is cachable.</summary>
    public TimeSpan? CacheControl { get; set; }

    /// <summary>The list of users with read permissions for the stream.</summary>
    public string[] AclRead { get; set; }

    /// <summary>The list of users with write permissions for the stream.</summary>
    public string[] AclWrite { get; set; }

    /// <summary>The list of users with delete permissions for the stream.</summary>
    public string[] AclDelete { get; set; }

    /// <summary>The list of users with write permissions to stream metadata for the stream.</summary>
    public string[] AclMetaRead { get; set; }

    /// <summary>The list of users with read permissions to stream metadata for the stream.</summary>
    public string[] AclMetaWrite { get; set; }

    /// <summary>The list of key-value pairs for user-provider metadata. <c>key1=value1,key2=value2;key3=value3</c> </summary>
    public string CustomMetadata { get; set; }
  }
}