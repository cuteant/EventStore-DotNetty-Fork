using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Transport.Http
{
    public class HttpAsyncClient : IHttpClient
    {
        private static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private HttpClient _client;

        static HttpAsyncClient()
        {
            ServicePointManager.MaxServicePointIdleTime = 10000;
            ServicePointManager.DefaultConnectionLimit = 800;
        }

        public HttpAsyncClient(TimeSpan timeout, HttpClientHandler clientHandler = null)
        {
            _client = clientHandler is null ? new HttpClient() : new HttpClient(clientHandler);
            _client.Timeout = timeout;
        }

        public void Get(string url, UserCredentials userCredentials,
            Action<HttpResponse> onSuccess, Action<Exception> onException,
            string hostHeader = "")
        {
            if (url is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.url); }
            if (onSuccess is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onSuccess); }
            if (onException is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onException); }

            Receive(HttpMethod.Get, url, userCredentials, onSuccess, onException, hostHeader);
        }

        public void Post(string url, string body, string contentType, UserCredentials userCredentials,
            Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            if (url is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.url); }
            if (body is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.body); }
            if (contentType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.contentType); }
            if (onSuccess is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onSuccess); }
            if (onException is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onException); }

            Send(HttpMethod.Post, url, body, contentType, userCredentials, onSuccess, onException);
        }

        public void Delete(string url, UserCredentials userCredentials,
            Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            if (url is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.url); }
            if (onSuccess is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onSuccess); }
            if (onException is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onException); }

            Receive(HttpMethod.Delete, url, userCredentials, onSuccess, onException);
        }

        public void Put(string url, string body, string contentType, UserCredentials userCredentials,
            Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            if (url is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.url); }
            if (body is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.body); }
            if (contentType is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.contentType); }
            if (onSuccess is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onSuccess); }
            if (onException is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onException); }

            Send(HttpMethod.Put, url, body, contentType, userCredentials, onSuccess, onException);
        }

        private void Receive(string method, string url, UserCredentials userCredentials,
            Action<HttpResponse> onSuccess, Action<Exception> onException, string hostHeader = "")
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = new System.Net.Http.HttpMethod(method);

            if (userCredentials is object)
                AddAuthenticationHeader(request, userCredentials);

            if (!string.IsNullOrWhiteSpace(hostHeader))
                request.Headers.Host = hostHeader;

            var state = new ClientOperationState(request, onSuccess, onException);
            _client.SendAsync(request).ContinueWith(RequestSent(state));
        }

        private void Send(string method, string url, string body, string contentType, UserCredentials userCredentials,
            Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = new System.Net.Http.HttpMethod(method);

            if (userCredentials is object)
                AddAuthenticationHeader(request, userCredentials);

            var bodyBytes = UTF8NoBom.GetBytes(body);
            var stream = new MemoryStream(bodyBytes);
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Headers.ContentLength = bodyBytes.Length;

            request.Content = content;

            var state = new ClientOperationState(request, onSuccess, onException);
            _client.SendAsync(request).ContinueWith(RequestSent(state));
        }

        private void AddAuthenticationHeader(HttpRequestMessage request, UserCredentials userCredentials)
        {
            if (userCredentials is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.userCredentials); }

            var httpAuthentication = string.Format("{0}:{1}", userCredentials.Username, userCredentials.Password);
            var encodedCredentials = Convert.ToBase64String(Helper.UTF8NoBom.GetBytes(httpAuthentication));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }

        private Action<Task<HttpResponseMessage>> RequestSent(ClientOperationState state)
        {
            return task =>
            {
                try
                {
                    var responseMsg = task.Result;
                    state.Response = new HttpResponse(responseMsg);
                    responseMsg.Content.ReadAsStringAsync()
                        .ContinueWith(ResponseRead(state));
                }
                catch (Exception ex)
                {
                    state.Dispose();
                    state.OnError(ex);
                }
            };
        }

        private Action<Task<string>> ResponseRead(ClientOperationState state)
        {
            return task =>
            {
                try
                {
                    state.Response.Body = task.Result;
                    state.Dispose();
                    state.OnSuccess(state.Response);
                }
                catch (Exception ex)
                {
                    state.Dispose();
                    state.OnError(ex);
                }
            };
        }
    }
}
