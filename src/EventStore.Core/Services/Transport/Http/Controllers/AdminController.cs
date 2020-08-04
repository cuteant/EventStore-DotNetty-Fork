using System;
using EventStore.Core.Bus;
using EventStore.Core.Messages;
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
            service.RegisterAction(new ControllerAction("/admin/shutdown", HttpMethod.Post, Codec.NoCodecs, SupportedCodecs, AuthorizationLevel.Ops), OnPostShutdown);
            service.RegisterAction(new ControllerAction("/admin/scavenge?startFromChunk={startFromChunk}&threads={threads}", HttpMethod.Post, Codec.NoCodecs, SupportedCodecs, AuthorizationLevel.Ops), OnPostScavenge);
            service.RegisterAction(new ControllerAction("/admin/scavenge/{scavengeId}", HttpMethod.Delete, Codec.NoCodecs, SupportedCodecs, AuthorizationLevel.Ops), OnStopScavenge);
            service.RegisterAction(new ControllerAction("/admin/mergeindexes", HttpMethod.Post, Codec.NoCodecs, SupportedCodecs, AuthorizationLevel.Ops), OnPostMergeIndexes);
        }

        private void OnPostShutdown(HttpEntityManager entity, UriTemplateMatch match)
        {
            if (entity.User is object && (entity.User.IsInRole(SystemRoles.Admins) || entity.User.IsInRole(SystemRoles.Operations)))
            {
                if (Log.IsInformationLevelEnabled()) Log.RequestShutDownOfNodeBecauseShutdownCommandHasBeenReceived();
                Publish(new ClientMessage.RequestShutdown(exitProcess: true, shutdownHttp: true));
                entity.ReplyStatus(HttpStatusCode.OK, "OK", exc => LogReplyError(exc));
            }
            else
            {
                entity.ReplyStatus(HttpStatusCode.Unauthorized, "Unauthorized", exc => LogReplyError(exc));
            }
        }

        private void OnPostMergeIndexes(HttpEntityManager entity, UriTemplateMatch match)
        {
            if (Log.IsInformationLevelEnabled()) Log.RequestMergeIndexesBecauseAdminMergeindexesRequestHasBeenReceived();

            var correlationId = Guid.NewGuid();
            var envelope = new SendToHttpEnvelope(_networkSendQueue, entity, (e, message) =>
            {
                return e.ResponseCodec.To(new MergeIndexesResultDto(correlationId.ToString()));
            },
                (e, message) =>
                {
                    var completed = message as ClientMessage.MergeIndexesResponse;
                    switch (completed?.Result)
                    {
                        case ClientMessage.MergeIndexesResponse.MergeIndexesResult.Started:
                            return Configure.Ok(e.ResponseCodec.ContentType);
                        default:
                            return Configure.InternalServerError();
                    }
                }
            );

            Publish(new ClientMessage.MergeIndexes(envelope, correlationId, entity.User));
        }

        private void OnPostScavenge(HttpEntityManager entity, UriTemplateMatch match)
        {
            int startFromChunk = 0;

            var startFromChunkVariable = match.BoundVariables["startFromChunk"];
            if (startFromChunkVariable is object)
            {
                if (!int.TryParse(startFromChunkVariable, out startFromChunk) || (uint)startFromChunk > Consts.TooBigOrNegative)
                {
                    SendBadRequest(entity, "startFromChunk must be a positive integer");
                    return;
                }
            }

            int threads = 1;

            var threadsVariable = match.BoundVariables["threads"];
            if (threadsVariable is object)
            {
                if (!int.TryParse(threadsVariable, out threads) || threads < 1)
                {
                    SendBadRequest(entity, "threads must be a 1 or above");
                    return;
                }
            }

            if (Log.IsInformationLevelEnabled()) Log.RequestScavengingBecauseRequestHasBeenReceived(startFromChunk, threads);

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

            Publish(new ClientMessage.ScavengeDatabase(envelope, Guid.Empty, entity.User, startFromChunk, threads));
        }

        private void OnStopScavenge(HttpEntityManager entity, UriTemplateMatch match)
        {
            var scavengeId = match.BoundVariables["scavengeId"];

            if (Log.IsInformationLevelEnabled()) Log.StoppingScavengeBecauseDeleteRequestHasBeenReceived(scavengeId);

            var envelope = new SendToHttpEnvelope(_networkSendQueue, entity, (e, message) =>
                {
                    var completed = message as ClientMessage.ScavengeDatabaseResponse;
                    return e.ResponseCodec.To(completed?.ScavengeId);
                },
                (e, message) =>
                {
                    var completed = message as ClientMessage.ScavengeDatabaseResponse;
                    switch (completed?.Result)
                    {
                        case ClientMessage.ScavengeDatabaseResponse.ScavengeResult.Stopped:
                            return Configure.Ok(e.ResponseCodec.ContentType);
                        case ClientMessage.ScavengeDatabaseResponse.ScavengeResult.Unauthorized:
                            return Configure.Unauthorized();
                        case ClientMessage.ScavengeDatabaseResponse.ScavengeResult.InvalidScavengeId:
                            return Configure.NotFound();
                        default:
                            return Configure.InternalServerError();
                    }
                }
            );

            Publish(new ClientMessage.StopDatabaseScavenge(envelope, Guid.Empty, entity.User, scavengeId));
        }

        private void LogReplyError(Exception exc)
        {
#if DEBUG
            if (Log.IsDebugLevelEnabled()) Log.Error_while_closing_HTTP_connection_admin_controller(exc);
#endif
        }
    }
}