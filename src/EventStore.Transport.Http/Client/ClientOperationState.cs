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
            if (null == request) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.request); }
            if (null == onSuccess) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onSuccess); }
            if (null == onError) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.onError); }

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