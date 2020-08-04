using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils.Threading;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using HttpStatusCode = EventStore.ClientAPI.Transport.Http.HttpStatusCode;

namespace EventStore.ClientAPI.PersistentSubscriptions
{
    internal class PersistentSubscriptionsClient
    {
        private readonly HttpAsyncClient _client;
        private readonly TimeSpan _operationTimeout;

        public PersistentSubscriptionsClient(ILogger log, TimeSpan operationTimeout)
        {
            _operationTimeout = operationTimeout;
            _client = new HttpAsyncClient(_operationTimeout);
        }

        public Task<PersistentSubscriptionDetails> Describe(EndPoint endPoint, string stream, string subscriptionName,
            UserCredentials userCredentials = null, string httpSchema = EndpointExtensions.HTTP_SCHEMA)
        {
            return SendGet(endPoint.ToHttpUrl(httpSchema, "/subscriptions/{0}/{1}/info", stream, subscriptionName),
                    userCredentials, HttpStatusCode.OK)
                .ContinueWith(x =>
                {
                    if (x.IsFaulted) throw x.Exception;
                    var r = JObject.Parse(x.Result);
                    return r is object ? r.ToObject<PersistentSubscriptionDetails>() : null;
                });
        }


        public Task<List<PersistentSubscriptionDetails>> List(EndPoint endPoint, string stream,
            UserCredentials userCredentials = null, string httpSchema = EndpointExtensions.HTTP_SCHEMA)
        {
            return SendGet(endPoint.ToHttpUrl(httpSchema, "/subscriptions/{0}", stream), userCredentials,
                    HttpStatusCode.OK)
                .ContinueWith(x =>
                {
                    if (x.IsFaulted) throw x.Exception;
                    var r = JArray.Parse(x.Result);
                    return r is object ? r.ToObject<List<PersistentSubscriptionDetails>>() : null;
                });
        }

        public Task<List<PersistentSubscriptionDetails>> List(EndPoint endPoint, UserCredentials userCredentials = null,
            string httpSchema = EndpointExtensions.HTTP_SCHEMA)
        {
            return SendGet(endPoint.ToHttpUrl(httpSchema, "/subscriptions"), userCredentials, HttpStatusCode.OK)
                .ContinueWith(x =>
                {
                    if (x.IsFaulted) throw x.Exception;
                    var r = JArray.Parse(x.Result);
                    return r is object ? r.ToObject<List<PersistentSubscriptionDetails>>() : null;
                });
        }

        public Task ReplayParkedMessages(EndPoint endPoint, string stream, string subscriptionName,
            UserCredentials userCredentials = null, string httpSchema = EndpointExtensions.HTTP_SCHEMA)
        {
            return SendPost(
                endPoint.ToHttpUrl(httpSchema, "/subscriptions/{0}/{1}/replayParked", stream, subscriptionName),
                string.Empty, userCredentials, HttpStatusCode.OK);
        }


        private Task<string> SendGet(string url, UserCredentials userCredentials, int expectedCode)
        {
            TaskCompletionSource<string> source = TaskCompletionSourceFactory.Create<string>();
            _client.Get(url, userCredentials, response =>
            {
                if (response.HttpStatusCode == expectedCode)
                    source.SetResult(response.Body);
                else
                    source.SetException(CoreThrowHelper.GetPersistentSubscriptionCommandFailedException_Get(response, url));
            }, new Action<Exception>(source.SetException), "");
            return source.Task;
        }


        private Task SendPost(string url, string content, UserCredentials userCredentials, int expectedCode)
        {
            TaskCompletionSource<object> source = TaskCompletionSourceFactory.Create<object>();
            _client.Post(url, content, "application/json", userCredentials, response =>
            {
                if (response.HttpStatusCode == expectedCode)
                    source.SetResult(null);
                else
                    source.SetException(CoreThrowHelper.GetPersistentSubscriptionCommandFailedException_Post(response, url));
            }, new Action<Exception>(source.SetException));
            return source.Task;
        }
        
    }
}