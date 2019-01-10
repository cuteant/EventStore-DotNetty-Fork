using System;
using System.IO;

namespace EventStore.Transport.Tcp
{
    public sealed class ClosedConnectionException : IOException
    {
        public ClosedConnectionException() { }

        public ClosedConnectionException(string message) : base(message) { }

        public ClosedConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
