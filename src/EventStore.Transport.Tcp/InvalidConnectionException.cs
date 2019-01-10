using System;

namespace EventStore.Transport.Tcp
{
    /// <summary>This exception is thrown when an association setup request is invalid and it is impossible to
    /// recover (malformed IP address, unknown hostname, etc...).</summary>
    public sealed class InvalidConnectionException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="InvalidConnectionException"/> class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public InvalidConnectionException(string message, Exception cause = null)
            : base(message, cause)
        {
        }
    }
}
