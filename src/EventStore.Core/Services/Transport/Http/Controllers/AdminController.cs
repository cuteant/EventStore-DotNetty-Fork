using System;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public class AdminController : CommunicationController
    {
        private readonly IPublisher _networkSendQueue;
        private static readonly ILogger Log = TraceLogger.GetLogger<AdminController>();

        private static readonly ICodec[] SupportedCodecs = new ICodec[] { Codec.Text, Codec.Json, Codec.Xml, Codec.ApplicationXml };

        public AdminController(IPublisher publisher, IPublisher networkSendQueue) : base(publisher)
        {
            _networkSendQueue = networkSendQueue;
        }

        protected override void SubscribeCore(IHttpService service)
        {
            service.RegisterAction(new ControllerAction("/admin/shutdown", HttpMethod.Post, Codec.NoCodecs, SupportedCodecs), OnPostShutdown);
            service.RegisterAction(new ControllerAction("/admin/scavenge", HttpMethod.Post, Codec.NoCodecs, SupportedCodecs), OnPostScavenge);
        }

        private void OnPostShutdown(HttpEntityManager entity, UriTemplateMatch match)
        {
            if (entity.User != null && (entity.User.IsInRole(SystemRoles.Admins) || entity.User.IsInRole(SystemRoles.Operations)))
            {
                if (Log.IsInformationLevelEnabled()) Log.LogInformation("Request shut down of node because shutdown command has been received.");
                Publish(new ClientMessage.RequestShutdown(exitProcess: true, shutdownHttp: true));
                entity.ReplyStatus(HttpStatusCode.OK, "OK", LogReplyError);
            }
            else
            {
                entity.ReplyStatus(HttpStatusCode.Unauthorized, "Unauthorized", LogReplyError);
            }
        }

        private void OnPostScavenge(HttpEntityManager entity, UriTemplateMatch match)
        {
            Log.Info("Request scavenging because /admin/scavenge request has been received.");

            var envelope = new SendToHttpEnvelope(_networkSendQueue, entity, (e, message) =>
                {
                    var completed = message as ClientMessage.ScavengeDatabaseResponse;
                    return e.ResponseCodec.To(new ScavengeResultDto(completed?.ScavengeId));
                },
                (e, message) =>
                {
                    var completed = message as ClientMessage.ScavengeDatabaseResponse;
                    switch (completed?.Result)
                    {
                        case ClientMessage.ScavengeDatabaseResponse.ScavengeResult.Started:
                            return Configure.Ok(e.ResponseCodec.ContentType);
                        case ClientMessage.ScavengeDatabaseResponse.ScavengeResult.InProgress:
                            return Configure.BadRequest();
                        case ClientMessage.ScavengeDatabaseResponse.ScavengeResult.Unauthorized:
                            return Configure.Unauthorized();
                        default:
                            return Configure.InternalServerError();
                    }
                }
            );
            
            Publish(new ClientMessage.ScavengeDatabase(envelope, Guid.Empty, entity.User));
        }

        private void LogReplyError(Exception exc)
        {
            if (Log.IsDebugLevelEnabled()) Log.LogDebug("Error while closing HTTP connection (admin controller): {0}.", exc.Message);
        }
    }
}