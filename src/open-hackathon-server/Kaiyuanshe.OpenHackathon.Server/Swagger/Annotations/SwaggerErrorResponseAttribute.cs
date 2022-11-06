using System;

namespace Kaiyuanshe.OpenHackathon.Server.Swagger.Annotations
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class SwaggerErrorResponseAttribute : Attribute
    {
        public int[] StatusCodes { get; }

        public SwaggerErrorResponseAttribute(params int[] statusCodes)
        {
            StatusCodes = statusCodes;
        }
    }
}