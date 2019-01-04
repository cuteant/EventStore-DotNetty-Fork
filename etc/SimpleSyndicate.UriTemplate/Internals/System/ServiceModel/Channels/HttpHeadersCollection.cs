using System.Collections.Specialized;
using System.Net;

namespace System.ServiceModel.Channels
{
    public class HttpHeadersCollection : NameValueCollection
    {
        public HttpHeadersCollection()
            : base()
        {
        }

        public string this[HttpRequestHeader header]
        {
            get
            {
                // headers are never populated
                return null;
            }
            set
            {
                // do nothing
            }
        }
    }
}
