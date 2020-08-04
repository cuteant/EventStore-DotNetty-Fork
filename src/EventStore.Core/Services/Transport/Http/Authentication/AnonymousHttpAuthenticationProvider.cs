using EventStore.Core.Services.Transport.Http.Messages;

namespace EventStore.Core.Services.Transport.Http.Authentication
{
    public class AnonymousHttpAuthenticationProvider : HttpAuthenticationProvider
    {
        public override bool Authenticate(IncomingHttpRequestMessage message)
        {
            var entity = message.Entity;
            if (entity.User is null)
            {
                Authenticated(message, user: null);
                return true;
            }
            return false;
        }
    }
}
