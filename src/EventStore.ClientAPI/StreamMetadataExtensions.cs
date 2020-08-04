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

            var builder = new StreamMetadataBuilder(
                maxCount: ConfigUtils.ToNullableInt64(metadata.MaxCount),
                maxAge: ConfigUtils.ToNullableTimeSpan(metadata.MaxAge, true),
                truncateBefore: ConfigUtils.ToNullableInt64(metadata.TruncateBefore),
                cacheControl: ConfigUtils.ToNullableTimeSpan(metadata.CacheControl, true),
                aclRead: Split(metadata.AclRead),
                aclWrite: Split(metadata.AclWrite),
                aclDelete: Split(metadata.AclDelete),
                aclMetaRead: Split(metadata.AclMetaRead),
                aclMetaWrite: Split(metadata.AclMetaWrite));

            if (!string.IsNullOrWhiteSpace(metadata.CustomMetadata))
            {
                String[] ss = metadata.CustomMetadata.Split(s_separators, StringSplitOptions.RemoveEmptyEntries);
                if (ss is object || (uint)ss.Length > 0u)
                {
                    foreach (var item in ss)
                    {
                        var kvs = item.Split(s_nameValueSeparator, StringSplitOptions.RemoveEmptyEntries);
                        if (kvs is object && (uint)kvs.Length > 0u)
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

        private static string[] Split(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) { return null; }
            return v.Split(s_separators, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
