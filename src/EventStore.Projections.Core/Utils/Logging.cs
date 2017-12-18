using System;
using System.Collections.Generic;

namespace EventStore.Projections.Core.Utils
{
    public static class Logging
    {
        //public static readonly string[] FilteredMessages = { "$get-state", "$state", "$get-result", "$result", "$statistics-report" };
        public static readonly HashSet<string> FilteredMessages = new HashSet<string>(new string[]
        {
            "$get-state", "$state", "$get-result", "$result", "$statistics-report"
        }, StringComparer.Ordinal);
    }
}
