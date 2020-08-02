using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Settings;
using EventStore.Transport.Http;
using EventStore.Transport.Http.Atom;
using EventStore.Transport.Http.Codecs;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;
using SpanJson.Serialization;

namespace EventStore.Core.Services.Transport.Http.Controllers
{
    public enum EmbedLevel
    {
        None,
        Content,
        Rich,
        Body,
        PrettyBody,
        TryHarder
    }

    public class AtomController : CommunicationController
    {
        public const char ETagSeparator = ';';
        public static readonly char[] ETagSeparatorArray = { ';' };

        private static readonly ILogger Log = TraceLogger.GetLogger<AtomController>();

        private static readonly HtmlFeedCodec HtmlFeedCodec = new HtmlFeedCodec(); // initialization order matters

        private static readonly ICodec[] AtomCodecsWithoutBatches =
        {
            Codec.EventStoreXmlCodec,
            Codec.EventStoreJsonCodec,
            Codec.Xml,
            Codec.ApplicationXml,
            Codec.Json
        };

        private static readonly ICodec[] AtomCodecs =
        {
            Codec.DescriptionJson,
            Codec.EventStoreXmlCodec,
            Codec.EventStoreJsonCodec,
            Codec.Xml,
            Codec.ApplicationXml,
            Codec.Json,
            Codec.EventXml,
            Codec.EventJson,
            Codec.EventsXml,
            Codec.EventsJson,
            Codec.Raw,
        };
        private static readonly ICodec[] AtomWithHtmlCodecs =
        {
            Codec.DescriptionJson,
            Codec.EventStoreXmlCodec,
            Codec.EventStoreJsonCodec,
            Codec.Xml,
            Codec.ApplicationXml,
            Codec.Json,
            Codec.EventXml,
            Codec.EventJson,
            Codec.EventsXml,
            Codec.EventsJson,
            HtmlFeedCodec // initialization order matters
        };

        private static readonly ICodec[] DefaultCodecs =
        {
            Codec.EventStoreXmlCodec,
            Codec.EventStoreJsonCodec,
            Codec.Xml,
            Codec.ApplicationXml,
            Codec.Json,
            Codec.EventXml,
            Codec.EventJson,
            Codec.Raw,
            HtmlFeedCodec // initialization order matters
        };

        private readonly IHttpForwarder _httpForwarder;
        private readonly IPublisher _networkSendQueue;

        public AtomController(IHttpForwarder httpForwarder, IPublisher publisher, IPublisher networkSendQueue, bool disableHTTPCaching = false) : base(publisher)
        {
            _httpForwarder = httpForwarder;
            _networkSendQueue = networkSendQueue;

            if (disableHTTPCaching)
            {
                Transport.Http.Configure.DisableHTTPCaching = true;
            }
        }

        protected override void SubscribeCore(IHttpService http)
        {
            // STREAMS
            Register(http, "/streams/{stream}", HttpMethod.Post, PostEvents, AtomCodecs, AtomCodecs);
            Register(http, "/streams/{stream}", HttpMethod.Delete, DeleteStream, Codec.NoCodecs, AtomCodecs);

            Register(http, "/streams/{stream}/incoming/{guid}", HttpMethod.Post, PostEventsIdempotent, AtomCodecsWithoutBatches, AtomCodecsWithoutBatches);

            Register(http, "/streams/{stream}/", HttpMethod.Post, RedirectKeepVerb, AtomCodecs, AtomCodecs);
            Register(http, "/streams/{stream}/", HttpMethod.Delete, RedirectKeepVerb, Codec.NoCodecs, AtomCodecs);
            Register(http, "/streams/{stream}/", HttpMethod.Get, RedirectKeepVerb, Codec.NoCodecs, AtomCodecs);

            Register(http, "/streams/{stream}?embed={embed}", HttpMethod.Get, GetStreamEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);

            Register(http, "/streams/{stream}/{event}?embed={embed}", HttpMethod.Get, GetStreamEvent, Codec.NoCodecs, DefaultCodecs);
            Register(http, "/streams/{stream}/{event}/{count}?embed={embed}", HttpMethod.Get, GetStreamEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/{stream}/{event}/backward/{count}?embed={embed}", HttpMethod.Get, GetStreamEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            RegisterCustom(http, "/streams/{stream}/{event}/forward/{count}?embed={embed}", HttpMethod.Get, GetStreamEventsForward, Codec.NoCodecs, AtomWithHtmlCodecs);

            // METASTREAMS
            Register(http, "/streams/{stream}/metadata", HttpMethod.Post, PostMetastreamEvent, AtomCodecs, AtomCodecs);
            Register(http, "/streams/{stream}/metadata/", HttpMethod.Post, RedirectKeepVerb, AtomCodecs, AtomCodecs);

            Register(http, "/streams/{stream}/metadata?embed={embed}", HttpMethod.Get, GetMetastreamEvent, Codec.NoCodecs, DefaultCodecs);
            Register(http, "/streams/{stream}/metadata/?embed={embed}", HttpMethod.Get, RedirectKeepVerb, Codec.NoCodecs, DefaultCodecs);
            Register(http, "/streams/{stream}/metadata/{event}?embed={embed}", HttpMethod.Get, GetMetastreamEvent, Codec.NoCodecs, DefaultCodecs);

            Register(http, "/streams/{stream}/metadata/{event}/{count}?embed={embed}", HttpMethod.Get, GetMetastreamEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/{stream}/metadata/{event}/backward/{count}?embed={embed}", HttpMethod.Get, GetMetastreamEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            RegisterCustom(http, "/streams/{stream}/metadata/{event}/forward/{count}?embed={embed}", HttpMethod.Get, GetMetastreamEventsForward, Codec.NoCodecs, AtomWithHtmlCodecs);

            // $ALL
            Register(http, "/streams/$all/", HttpMethod.Get, RedirectKeepVerb, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/%24all/", HttpMethod.Get, RedirectKeepVerb, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/$all?embed={embed}", HttpMethod.Get, GetAllEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/$all/{position}/{count}?embed={embed}", HttpMethod.Get, GetAllEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/$all/{position}/backward/{count}?embed={embed}", HttpMethod.Get, GetAllEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            RegisterCustom(http, "/streams/$all/{position}/forward/{count}?embed={embed}", HttpMethod.Get, GetAllEventsForward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/%24all?embed={embed}", HttpMethod.Get, GetAllEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/%24all/{position}/{count}?embed={embed}", HttpMethod.Get, GetAllEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            Register(http, "/streams/%24all/{position}/backward/{count}?embed={embed}", HttpMethod.Get, GetAllEventsBackward, Codec.NoCodecs, AtomWithHtmlCodecs);
            RegisterCustom(http, "/streams/%24all/{position}/forward/{count}?embed={embed}", HttpMethod.Get, GetAllEventsForward, Codec.NoCodecs, AtomWithHtmlCodecs);
        }

        private bool GetDescriptionDocument(HttpEntityManager manager, UriTemplateMatch match)
        {
            if (manager.ResponseCodec.ContentType == ContentType.DescriptionDocJson)
            {
                var stream = match.BoundVariables["stream"];
                var accepts = manager.HttpEntity.Request.AcceptTypes == null || manager.HttpEntity.Request.AcceptTypes.Contains(ContentType.Any);
                var responseStatusCode = accepts ? HttpStatusCode.NotAcceptable : HttpStatusCode.OK;
                var responseMessage = manager.HttpEntity.Request.AcceptTypes == null ? "We are unable to represent the stream in the format requested." : "Description Document";
                var envelope = new SendToHttpEnvelope(
                    _networkSendQueue, manager,
                    (args, message) =>
                    {
                        var m = message as MonitoringMessage.GetPersistentSubscriptionStatsCompleted;
                        if (m == null)
                        {
                            throw new Exception($"Could not get subscriptions for stream {stream}");
                        }

                        string[] persistentSubscriptionGroups = null;
                        if (m.Result == MonitoringMessage.GetPersistentSubscriptionStatsCompleted.OperationStatus.Success)
                        {
                            persistentSubscriptionGroups = m.SubscriptionStats.Select(x => x.GroupName).ToArray();
                        }
                        manager.ReplyTextContent(Format.GetDescriptionDocument(manager, stream, persistentSubscriptionGroups),
                                      responseStatusCode, responseMessage,
                                      manager.ResponseCodec.ContentType,
                                      null,
                                      e => Log.ErrorWhileWritingHttpResponse(e));
                        return String.Empty;
                    },
                    (args, message) => new ResponseConfiguration(HttpStatusCode.OK, manager.ResponseCodec.ContentType, manager.ResponseCodec.Encoding));
                var cmd = new MonitoringMessage.GetStreamPersistentSubscriptionStats(envelope, stream);
                Publish(cmd);
                return true;
            }
            return false;
        }

        private void RedirectKeepVerb(HttpEntityManager httpEntity, UriTemplateMatch uriTemplateMatch)
        {
            var original = uriTemplateMatch.RequestUri.ToString();
            var header = new[]
            {
                new KeyValuePair<string, string>("Location", original.Substring(0, original.Length - 1)),
                new KeyValuePair<string, string>("Cache-Control", "max-age=31536000, public"),
            };
            httpEntity.ReplyTextContent("Moved Permanently", HttpStatusCode.RedirectKeepVerb, "", "", header, e => { });
        }

        // STREAMS
        private void PostEvents(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            if (stream.IsEmptyString())
            {
                SendBadRequest(manager, "Invalid request. Stream must be non-empty string");
                return;
            }
            if (!GetIncludedType(manager, out string includedType))
            {
                SendBadRequest(manager, $"{SystemHeaders.EventType} header in wrong format.");
                return;
            }
            if (!manager.RequestCodec.HasEventTypes && includedType == null)
            {
                SendBadRequest(manager, "Must include an event type with the request either in body or as ES-EventType header.");
                return;
            }
            if (!GetIncludedId(manager, out Guid includedId))
            {
                SendBadRequest(manager, $"{SystemHeaders.EventId} header in wrong format.");
                return;
            }
            if (!manager.RequestCodec.HasEventIds && includedId == Guid.Empty)
            {
                var uri = new Uri(new Uri(match.RequestUri + "/"), "incoming/" + Guid.NewGuid()).ToString();
                var header = new[] { new KeyValuePair<string, string>("Location", uri) };
                manager.ReplyTextContent("Forwarding to idempotent URI", HttpStatusCode.RedirectKeepVerb, "Temporary Redirect", "text/plain", header, e => { });
                return;
            }
            if (!GetExpectedVersion(manager, out long expectedVersion))
            {
                SendBadRequest(manager, $"{SystemHeaders.ExpectedVersion} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }
            if (!requireMaster && _httpForwarder.ForwardRequest(manager)) { return; }
            PostEntry(manager, expectedVersion, requireMaster, stream, includedId, includedType);
        }

        private void PostEventsIdempotent(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var guid = match.BoundVariables["guid"];
            if (!Guid.TryParse(guid, out Guid id))
            {
                SendBadRequest(manager, "Invalid request. Unable to parse guid");
                return;
            }
            if (stream.IsEmptyString())
            {
                SendBadRequest(manager, "Invalid request. Stream must be non-empty string");
                return;
            }
            if (!GetIncludedType(manager, out string includedType))
            {
                SendBadRequest(manager, $"{SystemHeaders.EventType} header in wrong format.");
                return;
            }
            if (!GetExpectedVersion(manager, out long expectedVersion))
            {
                SendBadRequest(manager, $"{SystemHeaders.ExpectedVersion} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }
            if (!requireMaster && _httpForwarder.ForwardRequest(manager)) { return; }
            PostEntry(manager, expectedVersion, requireMaster, stream, id, includedType);
        }

        private void DeleteStream(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            if (stream.IsEmptyString())
            {
                SendBadRequest(manager, $"Invalid stream name '{stream}'");
                return;
            }
            if (!GetExpectedVersion(manager, out long expectedVersion))
            {
                SendBadRequest(manager, $"{SystemHeaders.ExpectedVersion} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }
            if (!GetHardDelete(manager, out bool hardDelete))
            {
                SendBadRequest(manager, $"{SystemHeaders.HardDelete} header in wrong format.");
                return;
            }
            if (!requireMaster && _httpForwarder.ForwardRequest(manager)) { return; }
            var envelope = new SendToHttpEnvelope(_networkSendQueue, manager, Format.DeleteStreamCompleted, Configure.DeleteStreamCompleted);
            var corrId = Guid.NewGuid();
            Publish(new ClientMessage.DeleteStream(corrId, corrId, envelope, requireMaster, stream, expectedVersion, hardDelete, manager.User));
        }

        private void GetStreamEvent(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var evNum = match.BoundVariables["event"];

            long eventNumber = -1;
            var embed = GetEmbedLevel(manager, match, EmbedLevel.TryHarder);

            if (stream.IsEmptyString())
            {
                SendBadRequest(manager, $"Invalid stream name '{stream}'");
                return;
            }
            if (evNum != "head" && (!long.TryParse(evNum, out eventNumber) || eventNumber < 0))
            {
                SendBadRequest(manager, $"'{evNum}' is not valid event number");
                return;
            }
            if (!GetResolveLinkTos(manager, out bool resolveLinkTos))
            {
                SendBadRequest(manager, $"{SystemHeaders.ResolveLinkTos} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }

            GetStreamEvent(manager, stream, eventNumber, resolveLinkTos, requireMaster, embed);
        }

        private void GetStreamEventsBackward(HttpEntityManager manager, UriTemplateMatch match)
        {
            if (GetDescriptionDocument(manager, match)) return;

            var stream = match.BoundVariables["stream"];
            var evNum = match.BoundVariables["event"];
            var cnt = match.BoundVariables["count"];

            long eventNumber = -1;
            int count = AtomSpecs.FeedPageSize;
            var embed = GetEmbedLevel(manager, match);

            if (stream.IsEmptyString())
            {
                SendBadRequest(manager, $"Invalid stream name '{stream}'");
                return;
            }
            if (evNum != null &&
              !string.Equals(evNum, "head", StringComparison.Ordinal) &&
              (!long.TryParse(evNum, out eventNumber) || eventNumber < 0))
            {
                SendBadRequest(manager, $"'{evNum}' is not valid event number");
                return;
            }
            if (cnt.IsNotEmptyString() && (!int.TryParse(cnt, out count) || (uint)(count - 1) >= Consts.TooBigOrNegative))
            {
                SendBadRequest(manager, $"'{cnt}' is not valid count. Should be positive integer");
                return;
            }
            if (!GetResolveLinkTos(manager, out bool resolveLinkTos))
            {
                SendBadRequest(manager, $"{SystemHeaders.ResolveLinkTos} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }

            bool headOfStream = eventNumber == -1;
            GetStreamEventsBackward(manager, stream, eventNumber, count, resolveLinkTos, requireMaster, headOfStream, embed);
        }

        private RequestParams GetStreamEventsForward(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var evNum = match.BoundVariables["event"];
            var cnt = match.BoundVariables["count"];

            var embed = GetEmbedLevel(manager, match);

            if (stream.IsEmptyString())
            {
                return SendBadRequest(manager, $"Invalid stream name '{stream}'");
            }

            if (evNum.IsEmptyString() || !long.TryParse(evNum, out long eventNumber) || eventNumber < 0)
            {
                return SendBadRequest(manager, $"'{evNum}' is not valid event number");
            }

            if (cnt.IsEmptyString() || !int.TryParse(cnt, out int count) || (uint)(count - 1) >= Consts.TooBigOrNegative)
            {
                return SendBadRequest(manager, $"'{cnt}' is not valid count. Should be positive integer");
            }

            if (!GetResolveLinkTos(manager, out bool resolveLinkTos))
            {
                return SendBadRequest(manager, $"{SystemHeaders.ResolveLinkTos} header in wrong format.");
            }

            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                return SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
            }

            if (!GetLongPoll(manager, out TimeSpan? longPollTimeout))
            {
                return SendBadRequest(manager, $"{SystemHeaders.LongPoll} header in wrong format.");
            }

            var etag = GetETagStreamVersion(manager);

            GetStreamEventsForward(manager, stream, eventNumber, count, resolveLinkTos, requireMaster, etag, longPollTimeout, embed);
            return new RequestParams((longPollTimeout ?? TimeSpan.Zero) + ESConsts.HttpTimeout);
        }

        // METASTREAMS
        private void PostMetastreamEvent(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            if (stream.IsEmptyString() || SystemStreams.IsMetastream(stream))
            {
                SendBadRequest(manager, string.Format("Invalid request. Stream must be non-empty string and should not be metastream"));
                return;
            }
            if (!GetIncludedId(manager, out Guid includedId))
            {
                SendBadRequest(manager, $"{SystemHeaders.EventId} header in wrong format.");
                return;
            }
            GetIncludedType(manager, out string foo);
            if (!(foo == null || foo == SystemEventTypes.StreamMetadata))
            {
                SendBadRequest(manager, "Bad Request. You should not include an event type for metadata.");
                return;
            }
            const string includedType = SystemEventTypes.StreamMetadata;
            if (!GetExpectedVersion(manager, out long expectedVersion))
            {
                SendBadRequest(manager, $"{SystemHeaders.ExpectedVersion} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }
            if (!requireMaster && _httpForwarder.ForwardRequest(manager)) { return; }
            PostEntry(manager, expectedVersion, requireMaster, SystemStreams.MetastreamOf(stream), includedId, includedType);
        }

        private void GetMetastreamEvent(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var evNum = match.BoundVariables["event"];

            long eventNumber = -1;
            var embed = GetEmbedLevel(manager, match, EmbedLevel.TryHarder);

            if (stream.IsEmptyString() || SystemStreams.IsMetastream(stream))
            {
                SendBadRequest(manager, "Stream must be non-empty string and should not be metastream");
                return;
            }
            if (evNum != null &&
              !string.Equals(evNum, "head", StringComparison.Ordinal) &&
              (!long.TryParse(evNum, out eventNumber) || eventNumber < 0))
            {
                SendBadRequest(manager, $"'{evNum}' is not valid event number");
                return;
            }
            if (!GetResolveLinkTos(manager, out bool resolveLinkTos))
            {
                SendBadRequest(manager, $"{SystemHeaders.ResolveLinkTos} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }

            GetStreamEvent(manager, SystemStreams.MetastreamOf(stream), eventNumber, resolveLinkTos, requireMaster, embed);
        }

        private void GetMetastreamEventsBackward(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var evNum = match.BoundVariables["event"];
            var cnt = match.BoundVariables["count"];

            long eventNumber = -1;
            int count = AtomSpecs.FeedPageSize;
            var embed = GetEmbedLevel(manager, match);

            if (stream.IsEmptyString() || SystemStreams.IsMetastream(stream))
            {
                SendBadRequest(manager, $"Invalid stream name '{stream}'");
                return;
            }
            if (evNum != null && evNum != "head" && (!long.TryParse(evNum, out eventNumber) || eventNumber < 0))
            {
                SendBadRequest(manager, $"'{evNum}' is not valid event number");
                return;
            }
            if (cnt.IsNotEmptyString() && (!int.TryParse(cnt, out count) || (uint)(count - 1) >= Consts.TooBigOrNegative))
            {
                SendBadRequest(manager, $"'{cnt}' is not valid count. Should be positive integer");
                return;
            }
            if (!GetResolveLinkTos(manager, out bool resolveLinkTos))
            {
                SendBadRequest(manager, $"{SystemHeaders.ResolveLinkTos} header in wrong format.");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }

            bool headOfStream = eventNumber == -1;
            GetStreamEventsBackward(manager, SystemStreams.MetastreamOf(stream), eventNumber, count,
                                    resolveLinkTos, requireMaster, headOfStream, embed);
        }

        private RequestParams GetMetastreamEventsForward(HttpEntityManager manager, UriTemplateMatch match)
        {
            var stream = match.BoundVariables["stream"];
            var evNum = match.BoundVariables["event"];
            var cnt = match.BoundVariables["count"];

            var embed = GetEmbedLevel(manager, match);

            if (stream.IsEmptyString() || SystemStreams.IsMetastream(stream))
            {
                return SendBadRequest(manager, $"Invalid stream name '{stream}'");
            }

            if (evNum.IsEmptyString() || !long.TryParse(evNum, out long eventNumber) || eventNumber < 0)
            {
                return SendBadRequest(manager, $"'{evNum}' is not valid event number");
            }

            if (cnt.IsEmptyString() || !int.TryParse(cnt, out int count) || (uint)(count - 1) >= Consts.TooBigOrNegative)
            {
                return SendBadRequest(manager, $"'{cnt}' is not valid count. Should be positive integer");
            }

            if (!GetResolveLinkTos(manager, out bool resolveLinkTos))
            {
                return SendBadRequest(manager, $"{SystemHeaders.ResolveLinkTos} header in wrong format.");
            }

            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                return SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
            }

            if (!GetLongPoll(manager, out TimeSpan? longPollTimeout))
            {
                return SendBadRequest(manager, $"{SystemHeaders.LongPoll} header in wrong format.");
            }

            var etag = GetETagStreamVersion(manager);

            GetStreamEventsForward(manager, SystemStreams.MetastreamOf(stream), eventNumber, count, resolveLinkTos,
                                   requireMaster, etag, longPollTimeout, embed);
            return new RequestParams((longPollTimeout ?? TimeSpan.Zero) + ESConsts.HttpTimeout);
        }

        // $ALL
        private void GetAllEventsBackward(HttpEntityManager manager, UriTemplateMatch match)
        {
            var pos = match.BoundVariables["position"];
            var cnt = match.BoundVariables["count"];

            TFPos position = TFPos.HeadOfTf;
            int count = AtomSpecs.FeedPageSize;
            var embed = GetEmbedLevel(manager, match);

            if (pos != null && !string.Equals(pos, "head", StringComparison.Ordinal)
                && (!TFPos.TryParse(pos, out position) || position.PreparePosition < 0 || position.CommitPosition < 0))
            {
                SendBadRequest(manager, $"Invalid position argument: {pos}");
                return;
            }
            if (cnt.IsNotEmptyString() && (!int.TryParse(cnt, out count) || (uint)(count - 1) >= Consts.TooBigOrNegative))
            {
                SendBadRequest(manager, $"Invalid count argument: {cnt}");
                return;
            }
            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
                return;
            }

            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  manager,
                                                  (args, msg) => Format.ReadAllEventsBackwardCompleted(args, msg, embed),
                                                  (args, msg) => Configure.ReadAllEventsBackwardCompleted(args, msg, position == TFPos.HeadOfTf));
            var corrId = Guid.NewGuid();
            Publish(new ClientMessage.ReadAllEventsBackward(corrId, corrId, envelope,
                                                            position.CommitPosition, position.PreparePosition, count,
                                                            requireMaster, true, GetETagTFPosition(manager), manager.User));
        }

        private RequestParams GetAllEventsForward(HttpEntityManager manager, UriTemplateMatch match)
        {
            var pos = match.BoundVariables["position"];
            var cnt = match.BoundVariables["count"];

            var embed = GetEmbedLevel(manager, match);

            if (!TFPos.TryParse(pos, out TFPos position) || position.PreparePosition < 0 || position.CommitPosition < 0)
            {
                return SendBadRequest(manager, $"Invalid position argument: {pos}");
            }

            if (!int.TryParse(cnt, out int count) || (uint)(count - 1) >= Consts.TooBigOrNegative)
            {
                return SendBadRequest(manager, $"Invalid count argument: {cnt}");
            }

            if (!GetRequireMaster(manager, out bool requireMaster))
            {
                return SendBadRequest(manager, $"{SystemHeaders.RequireMaster} header in wrong format.");
            }

            if (!GetLongPoll(manager, out TimeSpan? longPollTimeout))
            {
                return SendBadRequest(manager, $"{SystemHeaders.LongPoll} header in wrong format.");
            }

            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  manager,
                                                  (args, msg) => Format.ReadAllEventsForwardCompleted(args, msg, embed),
                                                  (args, msg) => Configure.ReadAllEventsForwardCompleted(args, msg, headOfTf: false));
            var corrId = Guid.NewGuid();
            Publish(new ClientMessage.ReadAllEventsForward(corrId, corrId, envelope,
                                                           position.CommitPosition, position.PreparePosition, count,
                                                           requireMaster, true, GetETagTFPosition(manager), manager.User,
                                                           longPollTimeout));
            return new RequestParams((longPollTimeout ?? TimeSpan.Zero) + ESConsts.HttpTimeout);
        }

        // HELPERS
        private bool GetExpectedVersion(HttpEntityManager manager, out long expectedVersion)
        {
            var expVer = manager.HttpEntity.Request.Headers[SystemHeaders.ExpectedVersion];
            if (expVer == null)
            {
                expectedVersion = ExpectedVersion.Any;
                return true;
            }
            return long.TryParse(expVer, out expectedVersion) && expectedVersion >= ExpectedVersion.StreamExists;
        }

        private bool GetIncludedId(HttpEntityManager manager, out Guid includedId)
        {
            var id = manager.HttpEntity.Request.Headers[SystemHeaders.EventId];
            if (id == null)
            {
                includedId = Guid.Empty;
                return true;
            }
            return Guid.TryParse(id, out includedId) && includedId != Guid.Empty;
        }
        private bool GetIncludedType(HttpEntityManager manager, out string includedType)
        {
            var type = manager.HttpEntity.Request.Headers[SystemHeaders.EventType];
            if (type == null)
            {
                includedType = null;
                return true;
            }
            includedType = type;
            return true;
        }


        private bool GetRequireMaster(HttpEntityManager manager, out bool requireMaster)
        {
            requireMaster = false;
            var onlyMaster = manager.HttpEntity.Request.Headers[SystemHeaders.RequireMaster];
            if (onlyMaster == null) { return true; }
            if (string.Equals(onlyMaster, "True", StringComparison.OrdinalIgnoreCase))
            {
                requireMaster = true;
                return true;
            }
            if (string.Equals(onlyMaster, "False", StringComparison.OrdinalIgnoreCase)) { return true; }
            return false;
        }

        private bool GetLongPoll(HttpEntityManager manager, out TimeSpan? longPollTimeout)
        {
            longPollTimeout = null;
            var longPollHeader = manager.HttpEntity.Request.Headers[SystemHeaders.LongPoll];
            if (longPollHeader == null) { return true; }
            if (int.TryParse(longPollHeader, out int longPollSec) && longPollSec > 0)
            {
                longPollTimeout = TimeSpan.FromSeconds(longPollSec);
                return true;
            }
            return false;
        }

        private bool GetResolveLinkTos(HttpEntityManager manager, out bool resolveLinkTos)
        {
            resolveLinkTos = true;
            var onlyMaster = manager.HttpEntity.Request.Headers[SystemHeaders.ResolveLinkTos];
            if (onlyMaster == null) { return true; }
            if (string.Equals(onlyMaster, "False", StringComparison.OrdinalIgnoreCase))
            {
                resolveLinkTos = false;
                return true;
            }
            if (string.Equals(onlyMaster, "True", StringComparison.OrdinalIgnoreCase)) { return true; }
            return false;
        }

        private bool GetHardDelete(HttpEntityManager manager, out bool hardDelete)
        {
            hardDelete = false;
            var hardDel = manager.HttpEntity.Request.Headers[SystemHeaders.HardDelete];
            if (hardDel == null) { return true; }
            if (string.Equals(hardDel, "True", StringComparison.OrdinalIgnoreCase))
            {
                hardDelete = true;
                return true;
            }
            if (string.Equals(hardDel, "False", StringComparison.OrdinalIgnoreCase)) { return true; }
            return false;
        }

        public void PostEntry(HttpEntityManager manager, long expectedVersion, bool requireMaster, string stream, Guid idIncluded, string typeIncluded)
        {
            //TODO GFY SHOULD WE MAKE THIS READ BYTE[] FOR RAW THEN CONVERT? AS OF NOW ITS ALL NO BOM UTF8
            manager.ReadRequestAsync(
                (man, body) =>
                {
                    var events = new Event[0];
                    try
                    {
                        events = AutoEventConverter.SmartParse(body, manager.RequestCodec, idIncluded, typeIncluded);
                    }
                    catch (Exception ex)
                    {
                        SendBadRequest(manager, ex.Message);
                        return;
                    }
                    if (events.IsEmpty())
                    {
                        SendBadRequest(manager, "Write request body invalid.");
                        return;
                    }
                    foreach (var e in events)
                    {
                        if (e.Data.Length + e.Metadata.Length > 4 * 1024 * 1024)
                        {
                            SendTooBig(manager);
                        }
                    }
                    var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                    manager,
                                                    Format.WriteEventsCompleted,
                                                    (a, m) => Configure.WriteEventsCompleted(a, m, stream));
                    var corrId = Guid.NewGuid();
                    var msg = new ClientMessage.WriteEvents(corrId, corrId, envelope, requireMaster,
                                                      stream, expectedVersion, events, manager.User);
                    Publish(msg);
                },
                e => { if (Log.IsDebugLevelEnabled()) Log.Error_while_reading_request_POST_entry(e); });
        }

        private void GetStreamEvent(HttpEntityManager manager, string stream, long eventNumber,
          bool resolveLinkTos, bool requireMaster, EmbedLevel embed)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  manager,
                                                  (args, message) => Format.EventEntry(args, message, embed),
                                                  (args, message) => Configure.EventEntry(args, message, headEvent: eventNumber == -1));
            var corrId = Guid.NewGuid();
            Publish(new ClientMessage.ReadEvent(corrId, corrId, envelope, stream, eventNumber, resolveLinkTos, requireMaster, manager.User));
        }

        private void GetStreamEventsBackward(HttpEntityManager manager, string stream, long eventNumber, int count,
          bool resolveLinkTos, bool requireMaster, bool headOfStream, EmbedLevel embed)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  manager,
                                                  (ent, msg) =>
                                                  Format.GetStreamEventsBackward(ent, msg, embed, headOfStream),
                                                  (args, msg) => Configure.GetStreamEventsBackward(args, msg, headOfStream));
            var corrId = Guid.NewGuid();
            Publish(new ClientMessage.ReadStreamEventsBackward(corrId, corrId, envelope, stream, eventNumber, count,
                                                               resolveLinkTos, requireMaster, GetETagStreamVersion(manager), manager.User));
        }

        private void GetStreamEventsForward(HttpEntityManager manager, string stream, long eventNumber, int count,
          bool resolveLinkTos, bool requireMaster, long? etag, TimeSpan? longPollTimeout, EmbedLevel embed)
        {
            var envelope = new SendToHttpEnvelope(_networkSendQueue,
                                                  manager,
                                                  (ent, msg) => Format.GetStreamEventsForward(ent, msg, embed),
                                                  Configure.GetStreamEventsForward);
            var corrId = Guid.NewGuid();
            Publish(new ClientMessage.ReadStreamEventsForward(corrId, corrId, envelope, stream, eventNumber, count,
                                                              resolveLinkTos, requireMaster, etag, manager.User, longPollTimeout));
        }

        private long? GetETagStreamVersion(HttpEntityManager manager)
        {
            var etag = manager.HttpEntity.Request.Headers["If-None-Match"];
            if (etag.IsNotEmptyString())
            {
                // etag format is version;contenttypehash
                var splitted = etag.Trim('\"').Split(ETagSeparatorArray);
                if ((uint)splitted.Length == 2u)
                {
                    var typeHash = manager.ResponseCodec.ContentType.GetHashCode().ToString(CultureInfo.InvariantCulture);
                    var res = splitted[1] == typeHash && long.TryParse(splitted[0], out long streamVersion) ? (long?)streamVersion : null;
                    return res;
                }
            }
            return null;
        }

        private static long? GetETagTFPosition(HttpEntityManager manager)
        {
            var etag = manager.HttpEntity.Request.Headers["If-None-Match"];
            if (etag.IsNotEmptyString())
            {
                // etag format is version;contenttypehash
                var splitted = etag.Trim('\"').Split(ETagSeparatorArray);
                if ((uint)splitted.Length == 2u)
                {
                    var typeHash = manager.ResponseCodec.ContentType.GetHashCode().ToString(CultureInfo.InvariantCulture);
                    return splitted[1] == typeHash && long.TryParse(splitted[0], out long tfEofPosition) ? (long?)tfEofPosition : null;
                }
            }
            return null;
        }

        private static readonly Dictionary<string, EmbedLevel> _rawValueMap =
            new Dictionary<string, EmbedLevel>(StringComparer.OrdinalIgnoreCase)
            {
                ["content"] = EmbedLevel.Content,
                ["rich"] = EmbedLevel.Rich,
                ["body"] = EmbedLevel.Body,
                ["pretty"] = EmbedLevel.PrettyBody,
                ["tryharder"] = EmbedLevel.TryHarder,
                ["rich"] = EmbedLevel.Rich,
            };
        private static EmbedLevel GetEmbedLevel(HttpEntityManager manager, UriTemplateMatch match,
          EmbedLevel htmlLevel = EmbedLevel.PrettyBody)
        {
            if (manager.ResponseCodec is IRichAtomCodec) { return htmlLevel; }
            var rawValue = match.BoundVariables["embed"] ?? string.Empty;
            if (_rawValueMap.TryGetValue(rawValue, out EmbedLevel embedLevel))
            {
                return embedLevel;
            }
            return EmbedLevel.None;
            //switch (rawValue.ToLowerInvariant())
            //{
            //  case "content": return EmbedLevel.Content;
            //  case "rich": return EmbedLevel.Rich;
            //  case "body": return EmbedLevel.Body;
            //  case "pretty": return EmbedLevel.PrettyBody;
            //  case "tryharder": return EmbedLevel.TryHarder;
            //  default: return EmbedLevel.None;
            //}
        }
    }

    internal class HtmlFeedCodec : ICodec, IRichAtomCodec
    {
        public string ContentType => "text/html";
        public Encoding Encoding => Helper.UTF8NoBom;
        public bool HasEventIds => false;
        public bool HasEventTypes => false;

        public bool CanParse(MediaType format)
        {
            throw new NotImplementedException();
        }

        public bool SuitableForResponse(MediaType component)
        {
            return component.Type == "*"
                   || (string.Equals(component.Type, "text", StringComparison.OrdinalIgnoreCase)
                       && (component.Subtype == "*" || string.Equals(component.Subtype, "html", StringComparison.OrdinalIgnoreCase)));
        }

        public T From<T>(string text)
        {
            throw new NotImplementedException();
        }

        public string To<T>(T value)
        {
            return @"
            <!DOCTYPE html>
            <html>
            <head>
            </head>
            <body>
            <script>
                var data = " + JsonCodec.IndentedSerializerPool.SerializeObject(value) + @";
                var newLocation = '/web/index.html#/streams/' + data.streamId" + @"
                if('positionEventNumber' in data){
                    newLocation = newLocation + '/' + data.positionEventNumber;
                }
                window.location.replace(newLocation);
            </script>
            </body>
            </html>
            ";
        }
    }

    interface IRichAtomCodec
    {
    }
}
