using System;
using System.Net;
using System.Runtime.CompilerServices;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Cluster;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Client;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;
using HttpStatusCode = EventStore.Transport.Http.HttpStatusCode;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public class GossipController :
        CommunicationController,
        IHttpSender,
        ISender<GossipMessage.SendGossip>
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<GossipController>();
        private static readonly ICodec[] SupportedCodecs = new ICodec[] { Codec.Json, Codec.ApplicationXml, Codec.Xml, Codec.Text };

        private readonly IPublisher _networkSendQueue;
        private readonly HttpAsyncClient _client;
        private readonly TimeSpan _gossipTimeout;

        public GossipController(IPublisher publisher, IPublisher networkSendQueue, TimeSpan gossipTimeout)
          : base(publisher)
        {
            _networkSendQueue = networkSendQueue;
            _gossipTimeout = gossipTimeout;
            _client = new HttpAsyncClient(_gossipTimeout);
        }

        protected override void SubscribeCore(IHttpService service)
        {
            service.RegisterAction(new ControllerAction("/gossip", HttpMethod.Get, Codec.NoCodecs, SupportedCodecs), OnGetGossip);
            if (service.Accessibility == ServiceAccessibility.Private)
            {
                service.RegisterAction(new ControllerAction("/gossip", HttpMethod.Post, SupportedCodecs, SupportedCodecs), OnPostGossip);
            }
        }

        public void SubscribeSenders(HttpMessagePipe pipe)
        {
            pipe.RegisterSender<GossipMessage.SendGossip>(this);
        }

        public void Send(GossipMessage.SendGossip message, IPEndPoint endPoint)
        {
            if (null == message) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.message); }
            if (null == endPoint) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.endPoint); }

            var url = endPoint.ToHttpUrl(EndpointExtensions.HTTP_SCHEMA, "/gossip");
            _client.Post(
                url,
                Codec.Json.To(new ClusterInfoDto(message.ClusterInfo, message.ServerEndPoint)),
                Codec.Json.ContentType,
                response =>
                {
                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        Publish(new GossipMessage.GossipSendFailed(
                            $"Received HTTP status code {response.HttpStatusCode}.", endPoint));
                        return;
                    }

                    var clusterInfo = Codec.Json.From<ClusterInfoDto>(response.Body);
                    if (clusterInfo == null)
                    {
                        OnClusterInfoDtoParseError(response, url, endPoint);
                        return;
                    }

                    Publish(new GossipMessage.GossipReceived(new NoopEnvelope(), new ClusterInfo(clusterInfo), endPoint));
                },
                error => Publish(new GossipMessage.GossipSendFailed(error.Message, endPoint)));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnClusterInfoDtoParseError(HttpResponse response, string url, IPEndPoint endPoint)
        {
            Log.ReceivedAsResponseInvalidClusterinfo(url, response);
            var msg = $"Received as RESPONSE invalid ClusterInfo from [{url}]. Content-Type: {response.ContentType}, Body:\n{response.Body}.";
            Publish(new GossipMessage.GossipSendFailed(msg, endPoint));
        }

        private void OnPostGossip(HttpEntityManager entity, UriTemplateMatch match)
        {
            entity.ReadTextRequestAsync(
                OnPostGossipRequestRead,
                e => { if (Log.IsDebugLevelEnabled()) Log.Error_while_reading_request_gossip(e); });
        }

        private void OnPostGossipRequestRead(HttpEntityManager manager, string body)
        {
            var clusterInfoDto = manager.RequestCodec.From<ClusterInfoDto>(body);
            if (clusterInfoDto == null)
            {
                OnClusterInfoDtoParseError(manager, body);
                return;
            }
            var sendToHttpEnvelope = new SendToHttpEnvelope(_networkSendQueue,
                                                            manager,
                                                            Format.SendGossip,
                                                            (e, m) => Configure.Ok(e.ResponseCodec.ContentType));
            var serverEndPoint = TryGetServerEndPoint(clusterInfoDto);
            Publish(new GossipMessage.GossipReceived(sendToHttpEnvelope, new ClusterInfo(clusterInfoDto), serverEndPoint));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnClusterInfoDtoParseError(HttpEntityManager manager, string body)
        {
            Log.ReceivedAsPostInvalidClusterinfo(manager, body);
            var msg = $"Received as POST invalid ClusterInfo from [{manager.RequestedUrl}]. Content-Type: {manager.RequestCodec.ContentType}, Body:\n{body}.";
            SendBadRequest(manager, msg);
        }

        private static IPEndPoint TryGetServerEndPoint(ClusterInfoDto clusterInfoDto)
        {
            IPEndPoint serverEndPoint = null;
            if (IPAddress.TryParse(clusterInfoDto.ServerIp, out IPAddress serverAddress)
              && clusterInfoDto.ServerPort > 0
              && clusterInfoDto.ServerPort <= 65535)
            {
                serverEndPoint = new IPEndPoint(serverAddress, clusterInfoDto.ServerPort);
            }
            return serverEndPoint;
        }

        private void OnGetGossip(HttpEntityManager entity, UriTemplateMatch match)
        {
            var sendToHttpEnvelope = new SendToHttpEnvelope(
                _networkSendQueue, entity, Format.SendGossip,
                (e, m) => Configure.Ok(e.ResponseCodec.ContentType, Helper.UTF8NoBom, null, null, false));
            Publish(new GossipMessage.GossipReceived(sendToHttpEnvelope, new ClusterInfo(new MemberInfo[0]), null));
        }
    }
}