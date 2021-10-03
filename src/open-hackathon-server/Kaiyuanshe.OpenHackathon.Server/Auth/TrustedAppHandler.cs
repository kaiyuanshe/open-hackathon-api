using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        IHttpContextAccessor _httpContextAccessor = null;
        string[] _trustedApps = new string[0];

        static readonly string HeaderNameAppId = "x-openhackathon-app";
        static readonly string configNameTrustedApps = "Guacamole:TrustedApps";

        public TrustedAppHandler(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            var trusted = configuration[configNameTrustedApps];
            if (trusted != null)
            {
                _trustedApps = trusted.Split(new char[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TrustedAppRequirement requirement)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            if (!request.Headers.ContainsKey(HeaderNameAppId))
            {
                // No "x-openhackathon-app" header
                return Task.CompletedTask;
            }

            var appId = request.Headers[HeaderNameAppId].ToString();
            if (_trustedApps.Any(id => id.Equals(appId, StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
