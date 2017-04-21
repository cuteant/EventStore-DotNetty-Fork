using System;
#if DESKTOPCLR
using System.Runtime.Serialization;
#endif

namespace EventStore.ClientAPI.Exceptions
{
  /// <summary>
  /// Exception thrown if a projection command fails.
  /// </summary>
  public class ProjectionCommandConflictException : ProjectionCommandFailedException
  {
    /// <summary>
    /// Constructs a new <see cref="ProjectionCommandFailedException"/>.
    /// </summary>
    public ProjectionCommandConflictException()
    {
    }

    /// <summary>
    /// Constructs a new <see cref="ProjectionCommandFailedException"/>.
    /// </summary>
    public ProjectionCommandConflictException(int httpStatusCode, string message)
      : base(httpStatusCode, message)
    {
    }

    /// <summary>
    /// Constructs a new <see cref="ProjectionCommandFailedException"/>.
    /// </summary>
    public ProjectionCommandConflictException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

#if DESKTOPCLR
    /// <summary>
    /// Constructs a new <see cref="ProjectionCommandFailedException"/>.
    /// </summary>
    protected ProjectionCommandConflictException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
#endif
  }
}