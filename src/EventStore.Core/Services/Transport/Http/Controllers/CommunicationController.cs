using System;
using EventStore.Core.Bus;
using EventStore.Core.Messaging;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public abstract class CommunicationController : IHttpController
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<CommunicationController>();
        private static readonly ICodec[] DefaultCodecs = new ICodec[] { Codec.Json, Codec.Xml };

        private readonly IPublisher _publisher;

        protected CommunicationController(IPublisher publisher)
        {
            if (null == publisher) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.publisher); }

            _publisher = publisher;
        }

        public void Publish(Message message)
        {
            if (null == message) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            _publisher.Publish(message);
        }

        public void Subscribe(IHttpService service)
        {
            if (null == service) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.service); }
            SubscribeCore(service);
        }

        protected abstract void SubscribeCore(IHttpService service);

        protected RequestParams SendBadRequest(HttpEntityManager httpEntityManager, string reason)
        {
            httpEntityManager.ReplyStatus(HttpStatusCode.BadRequest,
                                          reason,
                                          e => { if (Log.IsDebugLevelEnabled()) Log.Error_while_closing_HTTP_connection_bad_request(e); });
            return new RequestParams(done: true);
        }

        protected RequestParams SendTooBig(HttpEntityManager httpEntityManager)
        {
            httpEntityManager.ReplyStatus(HttpStatusCode.RequestEntityTooLarge,
                                          "Too large events received. Limit is 4mb",
                                          e => { if (Log.IsDebugLevelEnabled()) Log.Too_large_events_received_over_HTTP(); });
            return new RequestParams(done: true);
        }

        protected RequestParams SendOk(HttpEntityManager httpEntityManager)
        {
            httpEntityManager.ReplyStatus(HttpStatusCode.OK,
                                          "OK",
                                          e => { if (Log.IsDebugLevelEnabled()) Log.Error_while_closing_HTTP_connection_ok(e); });
            return new RequestParams(done: true);
        }

        protected void Register(IHttpService service, string uriTemplate, string httpMethod,
          Action<HttpEntityManager, UriTemplateMatch> handler, ICodec[] requestCodecs, ICodec[] responseCodecs)
        {
            service.RegisterAction(new ControllerAction(uriTemplate, httpMethod, requestCodecs, responseCodecs), handler);
        }

        protected void RegisterCustom(IHttpService service, string uriTemplate, string httpMethod,
          Func<HttpEntityManager, UriTemplateMatch, RequestParams> handler,
          ICodec[] requestCodecs, ICodec[] responseCodecs)
        {
            service.RegisterCustomAction(new ControllerAction(uriTemplate, httpMethod, requestCodecs, responseCodecs), handler);
        }

        protected void RegisterUrlBased(IHttpService service, string uriTemplate, string httpMethod,
          Action<HttpEntityManager, UriTemplateMatch> action)
        {
            Register(service, uriTemplate, httpMethod, action, Codec.NoCodecs, DefaultCodecs);
        }

        protected static string MakeUrl(HttpEntityManager http, string path)
        {
            if((uint)path.Length > 0u && path[0] == '/') path = path.Substring(1);
            var hostUri = http.ResponseUrl;
            var builder = new UriBuilder(hostUri.Scheme, hostUri.Host, hostUri.Port, hostUri.LocalPath + path);
            return builder.Uri.AbsoluteUri;
        }
    }
}