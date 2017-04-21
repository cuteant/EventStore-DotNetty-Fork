using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Transport.Tcp
{
  internal class PackageFramingException : Exception
  {
    public PackageFramingException()
    {
    }

    public PackageFramingException(string message) : base(message)
    {
    }

    public PackageFramingException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if DESKTOPCLR
    protected PackageFramingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
  }
}