using System;
using Microsoft.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Messages;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public class PingController : IHttpController
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<PingController>();

        private static readonly ICodec[] SupportedCodecs = new ICodec[] { Codec.Json, Codec.Xml, Codec.ApplicationXml, Codec.Text };

        public void Subscribe(IHttpService service)
        {
            Ensure.NotNull(service, "service");
            service.RegisterAction(new ControllerAction("/ping", HttpMethod.Get, Codec.NoCodecs, SupportedCodecs), OnGetPing);
        }

        private void OnGetPing(HttpEntityManager entity, UriTemplateMatch match)
        {
            var response = new HttpMessage.TextMessage("Ping request successfully handled");
            entity.ReplyTextContent(Format.TextMessage(entity, response),
                                    HttpStatusCode.OK,
                                    "OK",
                                    entity.ResponseCodec.ContentType,
                                    null,
                                    e => Log.LogError(e, "Error while writing HTTP response (ping)"));
        }
    }
}