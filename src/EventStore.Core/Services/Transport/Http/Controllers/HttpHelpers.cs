using System;
using System.Collections.Generic;
using EventStore.Common.Utils;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;
#if NETSTANDARD
using UriTemplate.Core;
#endif

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public static class HttpHelpers
    {
        private static readonly ILogger _logger = TraceLogger.GetLogger(typeof(HttpHelpers));

        public static void RegisterRedirectAction(IHttpService service, string fromUrl, string toUrl)
        {
            service.RegisterAction(
                new ControllerAction(
                    fromUrl,
                    HttpMethod.Get,
                    Codec.NoCodecs,
                    new ICodec[] { Codec.ManualEncoding }),
                    (http, match) => http.ReplyTextContent(
                        "Moved", 302, "Found", "text/plain",
                        new[]
                            {
                        new KeyValuePair<string, string>(
                            "Location",   new Uri(match.BaseUri, toUrl).AbsoluteUri)
                            }, ex => _logger.LogError(ex.ToString())));
        }

        public static void Reply(
            this HttpEntityManager http, string response, int code, string description, string contentType,
            IEnumerable<KeyValuePair<string, string>> headers = null)
        {
            http.Reply(Helper.UTF8NoBom.GetBytes(response), code, description, contentType, Helper.UTF8NoBom, headers, exception => { });
        }
    }
}