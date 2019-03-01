using System;

namespace EventStore.ClientAPI
{
  /// <summary>StreamMetadataExtensions</summary>
  public static class StreamMetadataExtensions
  {
    private static readonly char[] s_nameValueSeparator = new char[] { '=' };
    private static readonly char[] s_separators = new char[] { ',', ';' };

    /// <summary>ToStreamMetadata</summary>
    public static StreamMetadata ToStreamMetadata(this StreamMetadataAttribute metadata)
    {
      if (null == metadata) { return null; }

      var builder = new StreamMetadataBuilder(metadata.MaxCount, metadata.MaxAge, metadata.TruncateBefore, metadata.CacheControl,
          metadata.AclRead, metadata.AclWrite, metadata.AclDelete, metadata.AclMetaRead, metadata.AclMetaWrite);

      if (!string.IsNullOrWhiteSpace(metadata.CustomMetadata))
      {
        String[] ss = metadata.CustomMetadata.Split(s_separators, StringSplitOptions.RemoveEmptyEntries);
        if (ss != null || (uint)ss.Length > 0u)
        {
          foreach (var item in ss)
          {
            var kvs = item.Split(s_nameValueSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (kvs != null && (uint)kvs.Length > 0u)
            {
              if (kvs.Length == 2)
              {
                builder.SetCustomProperty(kvs[0], kvs[1]);
              }
              else
              {
                builder.SetCustomProperty(kvs[0], (string)null);
              }
            }
          }
        }
      }
      return builder.Build();
    }
  }
}
