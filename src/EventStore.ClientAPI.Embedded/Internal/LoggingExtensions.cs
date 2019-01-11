using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.Embedded
{
    internal static class EmbeddedLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheCoordinatorShardsStateWasSuccessfullyUpdatedWith(this ILogger logger, string newShard)
        {
            logger.LogDebug("The coordinator shards state was successfully updated with {0}", newShard);
        }
    }
}
