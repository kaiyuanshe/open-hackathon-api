using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Auth
{
    public class TrustedAppRequirement : IAuthorizationRequirement
    {

    }

    public class TrustedAppHandler : AuthorizationHandler<TrustedAppRequirement>
    {
        IHttpContextAccessor? _httpContextAccessor = null;
        string[] _trustedApps = new string[0];

        static readonly string HeaderNameAppId = "x-openhackathon-app-id";
        private readonly ILogger logger;

        public TrustedAppHandler(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TrustedAppHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            var trusted = configuration[ConfigurationKeys.GuacamoleTrustedApps];
            if (trusted != null)
            {
                _trustedApps = trusted.Split(new char[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            this.logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TrustedAppRequirement requirement)
        {
            var request = _httpContextAccessor?.HttpContext?.Request;
            if (request == null || !request.Headers.ContainsKey(HeaderNameAppId))
            {
                // No "x-openhackathon-app-id" header
                logger?.TraceInformation("No 'x-openhackathon-app-id' header found");
                return Task.CompletedTask;
            }

            var appId = request.Headers[HeaderNameAppId].ToString();
            if (_trustedApps.Any(id => id.Equals(appId, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
            }

            logger?.TraceInformation($"Untrusted app id: {appId}");
            return Task.CompletedTask;
        }
    }
}
