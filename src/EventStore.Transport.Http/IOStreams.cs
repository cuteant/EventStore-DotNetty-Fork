using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace EventStore.Transport.Http
{
    public class IOStreams
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<IOStreams>();

        public static void SafelyDispose(params Stream[] streams)
        {
            if (streams == null || streams.Length == 0)
                return;

            foreach (var stream in streams)
            {
                try
                {
                    if (stream != null)
                        stream.Dispose();
                }
                catch (Exception e)
                {
                    //Exceptions may be thrown when client shutdowned and we were unable to write all the data,
                    //Nothing we can do, ignore (another option - globally ignore write errors)
                    if (Log.IsInformationLevelEnabled()) Log.ErrorWhileClosingStream(e);
                }
            }
        }
    }
}
