using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using EventStore.Common.Utils;
using EventStore.Core.Bus;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Services.Histograms;
using EventStore.Core.Services.Transport.Http;
using EventStore.Transport.Http;
using EventStore.Transport.Http.EntityManagement;
using Microsoft.Extensions.Logging;
using HttpStatusCode = EventStore.Transport.Http.HttpStatusCode;

namespace EventStore.Core.Services
{
    public class HttpSendService :
        IHttpForwarder,
        IHandle<SystemMessage.StateChangeMessage>,
        IHandle<HttpMessage.SendOverHttp>,
        IHandle<HttpMessage.HttpSend>,
        IHandle<HttpMessage.HttpBeginSend>,
        IHandle<HttpMessage.HttpSendPart>,
        IHandle<HttpMessage.HttpEndSend>
    {
        private static readonly ILogger Log = TraceLogger.GetLogger<HttpSendService>();
        private static HttpClient _client = new HttpClient();

        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly HttpMessagePipe _httpPipe;
        private readonly bool _forwardRequests;
        private const string _httpSendHistogram = "http-send";
        private VNodeInfo _masterInfo;

        public HttpSendService(HttpMessagePipe httpPipe, bool forwardRequests)
        {
            Ensure.NotNull(httpPipe, "httpPipe");
            _httpPipe = httpPipe;
            _forwardRequests = forwardRequests;
        }

        public void Handle(SystemMessage.StateChangeMessage message)
        {
            switch (message.State)
            {
                case VNodeState.PreReplica:
                case VNodeState.CatchingUp:
                case VNodeState.Clone:
                case VNodeState.Slave:
                    _masterInfo = ((SystemMessage.ReplicaStateMessage)message).Master;
                    break;
                case VNodeState.Initializing:
                case VNodeState.Unknown:
                case VNodeState.PreMaster:
                case VNodeState.Master:
                case VNodeState.Manager:
                case VNodeState.ShuttingDown:
                case VNodeState.Shutdown:
                    _masterInfo = null;
                    break;
                default:
                    throw new Exception($"Unknown node state: {message.State}.");
            }
        }

        public void Handle(HttpMessage.SendOverHttp message)
        {
            if (message.LiveUntil > DateTime.Now)
            {
                _httpPipe.Push(message.Message, message.EndPoint);
            }
            else
            {
                if (Log.IsDebugLevelEnabled())
                {
                    Log.LogDebug("Dropping HTTP send message due to TTL being over. {1} To : {0}", message.EndPoint, message.Message.GetType().Name.ToString());
                }
            }
        }

        public void Handle(HttpMessage.HttpSend message)
        {
            if (message.Message is HttpMessage.DeniedToHandle deniedToHandle)
            {
                int code;
                switch (deniedToHandle.Reason)
                {
                    case DenialReason.ServerTooBusy:
                        code = HttpStatusCode.ServiceUnavailable;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var start = _watch.ElapsedTicks;
                message.HttpEntityManager.ReplyStatus(
                    code,
                    deniedToHandle.Details,
                    exc => Log.LogDebug("Error occurred while replying to HTTP with message {0}: {1}.", message.Message, exc.Message));
                HistogramService.SetValue(_httpSendHistogram,
                   (long)((((double)_watch.ElapsedTicks - start) / Stopwatch.Frequency) * 1000000000));
            }
            else
            {
                var response = message.Data;
                var config = message.Configuration;
                var start = _watch.ElapsedTicks;
                if (response is byte[])
                {
                    message.HttpEntityManager.ReplyContent(
                        response as byte[],
                        config.Code,
                        config.Description,
                        config.ContentType,
                        config.Headers,
                        exc => Log.LogDebug("Error occurred while replying to HTTP with message {0}: {1}.", message.Message, exc.Message));
                }
                else
                {
                    message.HttpEntityManager.ReplyTextContent(
                        response as string,
                        config.Code,
                        config.Description,
                        config.ContentType,
                        config.Headers,
                        exc => Log.LogDebug("Error occurred while replying to HTTP with message {0}: {1}.", message.Message, exc.Message));
                }
                HistogramService.SetValue(_httpSendHistogram,
                   (long)((((double)_watch.ElapsedTicks - start) / Stopwatch.Frequency) * 1000000000));

            }
        }

        public void Handle(HttpMessage.HttpBeginSend message)
        {
            var config = message.Configuration;

            message.HttpEntityManager.BeginReply(config.Code, config.Description, config.ContentType, config.Encoding, config.Headers);
            //if (message.Envelope != null)
            message.Envelope?.ReplyWith(new HttpMessage.HttpCompleted(message.CorrelationId, message.HttpEntityManager));
        }

        public void Handle(HttpMessage.HttpSendPart message)
        {
            var response = message.Data;
            message.HttpEntityManager.ContinueReplyTextContent(
                response,
                exc => Log.LogDebug("Error occurred while replying to HTTP with message {0}: {1}.", message, exc.Message),
                () =>
                {
              //if (message.Envelope != null)
              message.Envelope?.ReplyWith(new HttpMessage.HttpCompleted(message.CorrelationId, message.HttpEntityManager));
                });
        }

        public void Handle(HttpMessage.HttpEndSend message)
        {
            message.HttpEntityManager.EndReply();
            //if (message.Envelope != null)
            message.Envelope?.ReplyWith(new HttpMessage.HttpCompleted(message.CorrelationId, message.HttpEntityManager));
        }

        bool IHttpForwarder.ForwardRequest(HttpEntityManager manager)
        {
            var masterInfo = _masterInfo;
            if (_forwardRequests && masterInfo != null)
            {
                var srcUrl = manager.RequestedUrl;
                var srcBase = new Uri($"{srcUrl.Scheme}://{srcUrl.Host}:{srcUrl.Port}/", UriKind.Absolute);
                var baseUri = new Uri($"http://{masterInfo.InternalHttp}/");
                var forwardUri = new Uri(baseUri, srcBase.MakeRelativeUri(srcUrl));
                ForwardRequest(manager, forwardUri);
                return true;
            }
            return false;
        }

        private static void ForwardRequest(HttpEntityManager manager, Uri forwardUri)
        {
            var srcReq = manager.HttpEntity.Request;
            var request = new HttpRequestMessage()
            {
                RequestUri = forwardUri,
                Method = new System.Net.Http.HttpMethod(srcReq.HttpMethod)
            };

            var hasContentLength = false;
            // Copy unrestricted headers (including cookies, if any)
            foreach (var headerKey in srcReq.Headers.AllKeys)
            {
                try
                {
                    switch (headerKey.ToLowerInvariant())
                    {
                        case "accept": request.Headers.Accept.ParseAdd(srcReq.Headers[headerKey]); break;
                        case "connection": break;
                        case "content-type": break;
                        case "content-length": hasContentLength = true; break;
                        case "date": request.Headers.Date = DateTime.Parse(srcReq.Headers[headerKey]); break;
                        case "expect": break;
                        case "host": request.Headers.Host = forwardUri.Host; break;
                        case "if-modified-since": request.Headers.IfModifiedSince = DateTime.Parse(srcReq.Headers[headerKey]); break;
                        case "proxy-connection": break;
                        case "range": break;
                        case "referer": request.Headers.Referrer = new Uri(srcReq.Headers[headerKey]); break;
                        case "transfer-encoding": request.Headers.TransferEncoding.ParseAdd(srcReq.Headers[headerKey]); break;
                        case "user-agent": request.Headers.UserAgent.ParseAdd(srcReq.Headers[headerKey]); break;

                        default:
                            request.Headers.Add(headerKey, srcReq.Headers[headerKey]);
                            break;
                    }
                }
                catch (System.FormatException)
                {
                    request.Headers.TryAddWithoutValidation(headerKey, srcReq.Headers[headerKey]);
                }
            }

            if (!request.Headers.Contains(ProxyHeaders.XForwardedHost))
            {
                request.Headers.Add(ProxyHeaders.XForwardedHost, $"{manager.RequestedUrl.Host}:{manager.RequestedUrl.Port}");
            }

            // Copy content (if content body is allowed)
            if (!string.Equals(srcReq.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(srcReq.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase)
                && hasContentLength)
            {
                var streamContent = new StreamContent(srcReq.InputStream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(srcReq.ContentType);
                streamContent.Headers.ContentLength = srcReq.ContentLength64;
                request.Content = streamContent;

            }
            ForwardResponse(manager, request);
        }

        private static void ForwardReplyFailed(HttpEntityManager manager)
        {
            manager.ReplyStatus(HttpStatusCode.InternalServerError, "Error while forwarding request", _ => { });
        }

        private static Action<HttpEntityManager> s_forwardReplyFailed = ForwardReplyFailed;
        private static void ForwardResponse(HttpEntityManager manager, HttpRequestMessage request)
        {
            _client.SendAsync(request)
                   .ContinueWith((t, s) =>
                   {
                       var state = (Tuple<HttpEntityManager, Action<HttpEntityManager>, ILogger>)s;
                       var manager1 = state.Item1;

                       HttpResponseMessage response;
                       try
                       {
                           response = t.Result;
                       }
                       catch (Exception ex)
                       {
                           state.Item3.LogDebug("Error in SendAsync for forwarded request for '{0}': {1}.",
                                     manager1.RequestedUrl, ex.InnerException.Message);
                           state.Item2.Invoke(manager1);
                           return;
                       }

                       manager1.ForwardReply(response, exc => state.Item3.LogDebug("Error forwarding response for '{0}': {1}.", manager1.RequestedUrl, exc.Message));
                   }, Tuple.Create(manager, s_forwardReplyFailed, Log));
        }
    }
}
