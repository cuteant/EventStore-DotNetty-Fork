using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging
{
  /// <summary>ILogger extension methods for common scenarios.</summary>
  internal static class EsLoggerExtensions
  {
    #region -- Trace --

    /// <summary>Writes a trace log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="data">The message to log.</param>
    public static void LogTraceX(this ILogger logger, EventId eventId, Exception exception, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Trace, eventId, data, exception, _messageFormatter);
    }

    /// <summary>Writes a trace log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="data">The message to log.</param>
    public static void LogTraceX(this ILogger logger, EventId eventId, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Trace, eventId, data, null, _messageFormatter);
    }

    /// <summary>Writes a trace log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="data">The message to log.</param>
    public static void LogTraceX(this ILogger logger, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Trace, s_zero, data, null, _messageFormatter);
    }

    /// <summary>Writes a trace log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="data">The message to log.</param>
    public static void LogTraceX(this ILogger logger, Exception exception, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Trace, s_zero, data, exception, _messageFormatter);
    }

    #endregion

    #region -- Debug --

    /// <summary>Writes a debug log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="data">The message to log.</param>
    public static void LogDebugX(this ILogger logger, EventId eventId, Exception exception, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Debug, eventId, data, exception, _messageFormatter);
    }

    /// <summary>Writes a debug log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="data">The message to log.</param>
    public static void LogDebugX(this ILogger logger, EventId eventId, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Debug, eventId, data, null, _messageFormatter);
    }

    /// <summary>Writes a debug log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="data">The message to log.</param>
    public static void LogDebugX(this ILogger logger, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Debug, s_zero, data, null, _messageFormatter);
    }

    /// <summary>Formats and writes a debug log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="data">The message to log.</param>
    public static void LogDebugX(this ILogger logger, Exception exception, string data)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Debug, s_zero, data, exception, _messageFormatter);
    }

    #endregion

    #region -- Information --

    /// <summary>Writes an informational log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogInformationX(this ILogger logger, EventId eventId, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Information, eventId, message, exception, _messageFormatter);
    }

    /// <summary>Writes an informational log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogInformationX(this ILogger logger, EventId eventId, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Information, eventId, message, null, _messageFormatter);
    }

    /// <summary>Writes an informational log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">The message to log.</param>
    public static void LogInformationX(this ILogger logger, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Information, s_zero, message, null, _messageFormatter);
    }

    /// <summary>Writes an informational log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogInformationX(this ILogger logger, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Information, s_zero, message, exception, _messageFormatter);
    }

    #endregion

    #region -- Warning --

    /// <summary>Writes a warning log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogWarningX(this ILogger logger, EventId eventId, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Warning, eventId, message, exception, _messageFormatter);
    }

    /// <summary>Writes a warning log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogWarningX(this ILogger logger, EventId eventId, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Warning, eventId, message, null, _messageFormatter);
    }

    /// <summary>Writes a warning log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">The message to log.</param>
    public static void LogWarningX(this ILogger logger, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Warning, s_zero, message, null, _messageFormatter);
    }

    /// <summary>Writes a warning log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogWarningX(this ILogger logger, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Warning, s_zero, message, exception, _messageFormatter);
    }

    #endregion

    #region -- Error --

    /// <summary>Writes an error log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    public static void LogErrorX(this ILogger logger, EventId eventId, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Error, eventId, message, exception, _messageFormatter);
    }

    /// <summary>Writes an error log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogErrorX(this ILogger logger, EventId eventId, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Error, eventId, message, null, _messageFormatter);
    }

    /// <summary>Writes an error log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">The message to log.</param>
    public static void LogErrorX(this ILogger logger, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Error, s_zero, message, null, _messageFormatter);
    }

    /// <summary>Writes an error log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    public static void LogErrorX(this ILogger logger, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Error, s_zero, message, exception, _messageFormatter);
    }

    #endregion

    #region -- Critical --

    /// <summary>Writes a critical log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    public static void LogCriticalX(this ILogger logger, EventId eventId, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Critical, eventId, message, exception, _messageFormatter);
    }

    /// <summary>Writes a critical log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">The message to log.</param>
    public static void LogCriticalX(this ILogger logger, EventId eventId, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Critical, eventId, message, null, _messageFormatter);
    }

    /// <summary>Writes a critical log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">The message to log.</param>
    public static void LogCriticalX(this ILogger logger, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Critical, s_zero, message, null, _messageFormatter);
    }

    /// <summary>Writes a critical log message.</summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    public static void LogCriticalX(this ILogger logger, Exception exception, string message)
    {
      if (logger == null) { throw new ArgumentNullException(nameof(logger)); }

      logger.Log(LogLevel.Critical, s_zero, message, exception, _messageFormatter);
    }

    #endregion

    #region -- Helpers --

    private static readonly EventId s_zero = new EventId(0);

    private static readonly Func<object, Exception, string> _messageFormatter = MessageFormatter;

#if !NET40
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private static string MessageFormatter(object state, Exception error)
    {
      return state.ToString();
    }

    #endregion
  }
}
