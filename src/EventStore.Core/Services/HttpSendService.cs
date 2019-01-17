using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
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
            if (null == httpPipe) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.httpPipe); }
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
                    ThrowHelper.ThrowException_UnknownNodeState(message.State); break;
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
                if (Log.IsDebugLevelEnabled()) { Log.Dropping_HTTP_send_message_due_to_TTL_being_over(message); }
            }
        }

        public void Handle(HttpMessage.HttpSend message)
        {
            if (message.Message is HttpMessage.DeniedToHandle deniedToHandle)
            {
                int code = 0;
                switch (deniedToHandle.Reason)
                {
                    case DenialReason.ServerTooBusy:
                        code = HttpStatusCode.ServiceUnavailable;
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(); break;
                }
                var start = _watch.ElapsedTicks;
                message.HttpEntityManager.ReplyStatus(
                    code,
                    deniedToHandle.Details,
                    exc => { if (Log.IsDebugLevelEnabled()) Log.Error_occurred_while_replying_to_HTTP_with_message(message, exc); });
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
                        exc => { if (Log.IsDebugLevelEnabled()) Log.Error_occurred_while_replying_to_HTTP_with_message(message, exc); });
                }
                else
                {
                    message.HttpEntityManager.ReplyTextContent(
                        response as string,
                        config.Code,
                        config.Description,
                        config.ContentType,
                        config.Headers,
                        exc => { if (Log.IsDebugLevelEnabled()) Log.Error_occurred_while_replying_to_HTTP_with_message(message, exc); });
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
                exc => { if (Log.IsDebugLevelEnabled()) Log.Error_occurred_while_replying_to_HTTP_with_message(message, exc); },
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
                streamContent.Headers.ContentLength = srcReq.ContentLength64;
                request.Content = streamContent;

                MediaTypeHeaderValue contentType;
                if (MediaTypeHeaderValue.TryParse(srcReq.ContentType, out contentType))
                {
                    streamContent.Headers.ContentType = contentType;
                }
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
                           if (state.Item3.IsDebugLevelEnabled()) state.Item3.Error_in_SendAsync_for_forwarded_request_for(manager1.RequestedUrl, ex);
                           state.Item2.Invoke(manager1);
                           return;
                       }

                       manager1.ForwardReply(response, exc => { if (state.Item3.IsDebugLevelEnabled()) state.Item3.Error_forwarding_response_for(manager1.RequestedUrl, exc); });
                   }, Tuple.Create(manager, s_forwardReplyFailed, Log));
        }
    }
}
