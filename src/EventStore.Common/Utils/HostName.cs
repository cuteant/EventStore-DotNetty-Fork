using System;
using System.Diagnostics;

namespace EventStore.Common.Utils
{
    public class HostName
    {
        public static string Combine(Uri responseUrl, string relativeUri, params object[] arg)
        {
            try
            {
                return CombineHostNameAndPath(responseUrl, relativeUri, arg);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to combine hostname with relative path: {0}", e.Message);
                return relativeUri;
            }
        }

        private static string CombineHostNameAndPath(Uri responseUrl,
                                                     string relativeUri,
                                                     object[] arg)
        {
            //TODO: encode???
            var path = string.Format(relativeUri, arg);
            if((uint)path.Length > 0u && path[0] == '/') path = path.Substring(1);
            return new UriBuilder(responseUrl.Scheme, responseUrl.Host, responseUrl.Port, responseUrl.LocalPath + path).Uri.AbsoluteUri;
        }
    }
}
