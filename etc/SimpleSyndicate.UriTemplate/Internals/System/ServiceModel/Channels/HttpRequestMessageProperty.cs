namespace System.ServiceModel.Channels
{
    public class HttpRequestMessageProperty
    {
        private HttpHeadersCollection _httpHeadersCollection;

        public HttpRequestMessageProperty()
        {
            _httpHeadersCollection = new HttpHeadersCollection();
        }

        public HttpHeadersCollection Headers
        {
            get { return _httpHeadersCollection; }
        }
    }
}
