using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Swagger.SchemaFilters
{
    /// <summary>
    /// Add reference/link to external types.
    /// </summary>
    public class ExternalTypeSchemaFilter : ISchemaFilter
    {
        static Dictionary<Type, string> externalTypeDescriptions = new Dictionary<Type, string>
        {
            [typeof(ProblemDetails)] =
                "A machine-readable format for specifying errors in HTTP API responses. " +
                "<a target=\"blank\" href=\"https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails?view=aspnetcore-6.0\">See more</a>.",
        };

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (externalTypeDescriptions.ContainsKey(context.Type))
            {
                schema.Description = externalTypeDescriptions[context.Type];
            }
        }
    }
}
