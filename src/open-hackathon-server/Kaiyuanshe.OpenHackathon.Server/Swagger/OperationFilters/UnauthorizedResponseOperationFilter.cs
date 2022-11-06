using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kaiyuanshe.OpenHackathon.Server.Swagger.OperationFilters
{
    /// <summary>
    /// Add 401 response to schema for all methods annotated with <seealso cref="AuthorizeAttribute"/>
    /// </summary>
    public class UnauthorizedResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses.ContainsKey("401"))
            {
                // in case 401 is explicitly added
                return;
            }

            var errorRespSchema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
            var authorizeAttrs = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>();
            if (authorizeAttrs.Count() > 0)
            {
                // Add 401 response to Swagger
                operation.Responses.Add("401", new OpenApiResponse
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = errorRespSchema
                        }
                    },
                    Description = ErrorResponseOperationFilter.statusCodeDescriptions[401],
                });

                // Add Required header
                if (operation.Parameters == null)
                {
                    operation.Parameters = new List<OpenApiParameter>();
                }
            }
        }
    }
}
