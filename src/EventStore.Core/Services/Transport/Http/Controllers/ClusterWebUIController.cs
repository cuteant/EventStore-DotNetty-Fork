using System;
using System.Collections.Generic;
using EventStore.Core.Bus;
using EventStore.Core.Util;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public class ClusterWebUiController : CommunicationController
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<ClusterWebUiController>();

        private readonly NodeSubsystems[] _enabledNodeSubsystems;

        //private readonly MiniWeb _commonWeb;
        private readonly MiniWeb _clusterNodeWeb;

        public ClusterWebUiController(IPublisher publisher, NodeSubsystems[] enabledNodeSubsystems)
          : base(publisher)
        {
            _enabledNodeSubsystems = enabledNodeSubsystems;
            _clusterNodeWeb = new MiniWeb("/web");
        }

        protected override void SubscribeCore(IHttpService service)
        {
            _clusterNodeWeb.RegisterControllerActions(service);
            RegisterRedirectAction(service, "", "/web/index.html");
            RegisterRedirectAction(service, "/web", "/web/index.html");

            service.RegisterAction(
                new ControllerAction("/sys/subsystems", HttpMethod.Get, Codec.NoCodecs, new ICodec[] { Codec.Json }, AuthorizationLevel.Ops),
                OnListNodeSubsystems);
        }

        private void OnListNodeSubsystems(HttpEntityManager http, UriTemplateMatch match)
        {
            http.ReplyTextContent(
              Codec.Json.To(_enabledNodeSubsystems),
              200,
              "OK",
              "application/json",
              null,
              ex => { if (Log.IsInformationLevelEnabled()) Log.FailedToPrepareMainMenu(ex); });
        }

        private static void RegisterRedirectAction(IHttpService service, string fromUrl, string toUrl)
        {
            service.RegisterAction(
                new ControllerAction(
                    fromUrl,
                    HttpMethod.Get,
                    Codec.NoCodecs,
                    new ICodec[] { Codec.ManualEncoding },
                    AuthorizationLevel.None),
                    (http, match) => http.ReplyTextContent(
                        "Moved", 302, "Found", "text/plain",
                        new[]
                        {
                    new KeyValuePair<string, string>("Location",   new Uri(http.HttpEntity.RequestedUrl, toUrl).AbsoluteUri)
                        }, ex => Log.LogError(ex.ToString())));
        }
    }
}
