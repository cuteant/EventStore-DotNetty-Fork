using System;
using System.Net.Http;

namespace EventStore.Transport.Http.Client
{
    public class ClientOperationState
    {
        public readonly HttpRequestMessage Request;
        public readonly Action<HttpResponse> OnSuccess;
        public readonly Action<Exception> OnError;

        public HttpResponse Response { get; set; }

        public ClientOperationState(HttpRequestMessage request, Action<HttpResponse> onSuccess, Action<Exception> onError)
        {
            if (request is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.request); }
            if (onSuccess is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onSuccess); }
            if (onError is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onError); }

            Request = request;
            OnSuccess = onSuccess;
            OnError = onError;
        }

        public void Dispose()
        {
            Request.Dispose();
        }
    }
}