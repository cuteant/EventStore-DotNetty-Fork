using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>
  /// Exception thrown if the expected version specified on an operation
  /// does not match the version of the stream when the operation was attempted. 
  /// </summary>
  public class WrongExpectedVersionException : EventStoreConnectionException
  {
    /// <summary>
    /// Constructs a new instance of <see cref="WrongExpectedVersionException" />.
    /// </summary>
    public WrongExpectedVersionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs a new instance of <see cref="WrongExpectedVersionException" />.
    /// </summary>
    public WrongExpectedVersionException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if DESKTOPCLR
    /// <summary>
    /// Constructs a new instance of <see cref="WrongExpectedVersionException" />.
    /// </summary>
    protected WrongExpectedVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
  }
}
