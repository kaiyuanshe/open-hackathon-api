using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        public static void TraceInformation(this ILogger logger, string message)
        {
            string traceId = Activity.Current?.Id ?? string.Empty;
            string messageWithTraceId = $"[{traceId}] {message}";
            logger.LogInformation(messageWithTraceId);
        }

        public static void TraceError(this ILogger logger, string message, Exception exception)
        {
            string traceId = Activity.Current?.Id ?? string.Empty;
            string messageWithTraceId = $"[{traceId}] {message}";
            logger.LogError(exception, messageWithTraceId);
        }
    }
}
