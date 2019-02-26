using System;

namespace EventStore.ClientAPI
{
    /// <summary>TBD</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public class StreamAttribute : Attribute
    {
        public readonly string StreamId;

        public StreamAttribute(string stream)
        {
            if (string.IsNullOrEmpty(stream)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream); }

            StreamId = stream;
        }
    }
}