using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Middlewares
{
    public class HttpHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public HttpHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            context.Response.Headers.Add("traceid", Activity.Current?.Id ?? string.Empty);
            return _next(context);
        }
    }
}
