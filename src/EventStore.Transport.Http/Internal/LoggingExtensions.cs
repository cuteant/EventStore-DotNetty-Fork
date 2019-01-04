using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Http
{
    internal static class TransportHttpLoggingExtensions
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void TheCoordinatorShardsStateWasSuccessfullyUpdatedWith(this ILogger logger, string newShard)
        {
            logger.LogDebug("The coordinator shards state was successfully updated with {0}", newShard);
        }
    }
}
