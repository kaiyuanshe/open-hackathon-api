using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public class TraceIdHttpPipelinePolicy : HttpPipelineSynchronousPolicy
    {
        private readonly ILogger logger;

        public TraceIdHttpPipelinePolicy(ILogger<TraceIdHttpPipelinePolicy> logger)
        {
            this.logger = logger;
        }


        public override void OnSendingRequest(HttpMessage message)
        {
            string traceId = Activity.Current?.Id ?? string.Empty;
            if (message.TryGetProperty(HttpHeaderNames.TraceId, out object? prop))
            {
                if (prop != null)
                {
                    traceId = (string)prop;
                    message.Request.Headers.SetValue(HttpHeaderNames.TraceId, traceId);
                }
            }

            if (EnableLogging(message))
            {
                logger.LogInformation($"[{traceId}]OnSendingRequest. " +
                    $"Path={GetPath(message)};" +
                    $"Method={message.Request.Method};" +
                    $"TimeStamp:{DateTime.UtcNow.ToString("O")}");
            }
        }

        public override void OnReceivedResponse(HttpMessage message)
        {
            if (!EnableLogging(message))
            {
                return;
            }

            string traceId = Activity.Current?.Id ?? string.Empty;
            if (message.TryGetProperty(HttpHeaderNames.TraceId, out object? prop))
            {
                if (prop != null)
                {
                    traceId = (string)prop;
                }
            }

            if (!message.HasResponse)
            {
                logger.LogInformation($"[{traceId}]OnReceivedResponse. " +
                    $"HasResponse=false;Path={GetPath(message)};" +
                    $"Method={message.Request.Method};" +
                    $"TimeStamp:{DateTime.UtcNow.ToString("O")}");
            }
            else
            {
                var builder = new StringBuilder();
                builder.Append($"[{traceId}]OnReceivedResponse. HasResponse=true;")
                    .Append($"Path={GetPath(message)};")
                    .Append($"Method={message.Request.Method};")
                    .Append($"TimeStamp={DateTime.UtcNow.ToString("O")};")
                    .Append($"Status={message.Response.Status};")
                    .Append($"ReasonPhrase={message.Response.ReasonPhrase};");
                logger.LogInformation(builder.ToString());
            }
        }

        private bool EnableLogging(HttpMessage message)
        {
            Debug.Assert(message.Request.Uri.Host != null);

            // skip Qeueu messages GET, there are too many.
            string host = message.Request.Uri.Host.ToLower();
            string method = message.Request.Method.Method.ToUpper();
            if (host.Substring(host.IndexOf(".")).StartsWith(".queue.") && method == "GET")
            {
                return false;
            }

            return true;
        }

        private string GetPath(HttpMessage message)
        {
            // skip query which may contains credentials
            return message.Request.Uri.ToUri().GetLeftPart(UriPartial.Path);
        }
    }

    public interface ITraceIdHttpPipelinePolicyFactory
    {
        TraceIdHttpPipelinePolicy GetPipelinePolicy();
    }

    public class TraceIdHttpPipelinePolicyFactory : ITraceIdHttpPipelinePolicyFactory
    {
        TraceIdHttpPipelinePolicy policy;

        public TraceIdHttpPipelinePolicyFactory(ILoggerFactory factory)
        {
            var logger = factory.CreateLogger<TraceIdHttpPipelinePolicy>();
            policy = new TraceIdHttpPipelinePolicy(logger);
        }

        public TraceIdHttpPipelinePolicy GetPipelinePolicy()
        {
            return policy;
        }
    }
}
