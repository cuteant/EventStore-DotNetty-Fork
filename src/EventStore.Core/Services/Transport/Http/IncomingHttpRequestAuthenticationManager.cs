using EventStore.Core.Bus;
using EventStore.Core.Services.Transport.Http.Authentication;
using EventStore.Core.Services.Transport.Http.Messages;

namespace EventStore.Core.Services.Transport.Http
{
    class IncomingHttpRequestAuthenticationManager : IHandle<IncomingHttpRequestMessage>
    {
        private readonly HttpAuthenticationProvider[] _providers;

        public IncomingHttpRequestAuthenticationManager(HttpAuthenticationProvider[] providers)
        {
            _providers = providers;
        }

        public void Handle(IncomingHttpRequestMessage message)
        {
            Authenticate(message);
        }

        private void Authenticate(IncomingHttpRequestMessage message)
        {
            try
            {
                for (int idx = 0; idx < _providers.Length; idx++)
                {
                    if (_providers[idx].Authenticate(message)) { break; }
                }
            }
            catch 
            {
                HttpAuthenticationProvider.ReplyUnauthorized(message.Entity);
            }
        }

    }
}
