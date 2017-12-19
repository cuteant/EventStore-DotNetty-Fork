﻿using System;
using EventStore.Common.Utils;
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
            Ensure.NotNull(publisher, nameof(publisher));

            _publisher = publisher;
        }

        public void Publish(Message message)
        {
            Ensure.NotNull(message, nameof(message));
            _publisher.Publish(message);
        }

        public void Subscribe(IHttpService service)
        {
            Ensure.NotNull(service, nameof(service));
            SubscribeCore(service);
        }

        protected abstract void SubscribeCore(IHttpService service);

        protected RequestParams SendBadRequest(HttpEntityManager httpEntityManager, string reason)
        {
            httpEntityManager.ReplyStatus(HttpStatusCode.BadRequest,
                                          reason,
                                          e =>
                                          {
                                              if (Log.IsDebugLevelEnabled()) Log.LogDebug("Error while closing HTTP connection (bad request): {0}.", e.Message);
                                          });
            return new RequestParams(done: true);
        }

        protected RequestParams SendTooBig(HttpEntityManager httpEntityManager)
        {
            httpEntityManager.ReplyStatus(HttpStatusCode.RequestEntityTooLarge,
                                          "Too large events received. Limit is 4mb",
                                          e =>
                                          {
                                              if (Log.IsDebugLevelEnabled()) Log.LogDebug("Too large events received over HTTP");
                                          });
            return new RequestParams(done: true);
        }

        protected RequestParams SendOk(HttpEntityManager httpEntityManager)
        {
            httpEntityManager.ReplyStatus(HttpStatusCode.OK,
                                          "OK",
                                          e =>
                                          {
                                              if (Log.IsDebugLevelEnabled()) Log.LogDebug("Error while closing HTTP connection (ok): {0}.", e.Message);
                                          });
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
            var hostUri = http.RequestedUrl;
            return new UriBuilder(hostUri.Scheme, hostUri.Host, hostUri.Port, path).Uri.AbsoluteUri;
        }
    }
}