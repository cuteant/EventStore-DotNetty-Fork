using System;
using System.Net;

namespace EventStore.ClientAPI.Transport.Http
{
    internal static class WebRequestExtensions
    {
        public static WebResponse EndGetResponseExtended(this WebRequest request, IAsyncResult asyncResult)
        {
            try
            {
                return request.EndGetResponse(asyncResult);
            }
            catch (WebException e)
            {
                if (e.Response is object)
                    return e.Response; //for 404 and 500
                throw;
            }
        }
    }
}
