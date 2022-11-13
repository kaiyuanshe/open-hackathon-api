using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace Kaiyuanshe.OpenHackathon.Server.Swagger.DocumentFilters
{
    /// <summary>
    /// Append Xml comments of EnumType to operation parameters.
    /// </summary>
    public class EnumParameterCommentsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var parameter in operation.Parameters)
            {
                var schemaReferenceId = parameter.Schema.Reference?.Id;
                if (string.IsNullOrEmpty(schemaReferenceId))
                {
                    schemaReferenceId = parameter.Schema.AllOf.FirstOrDefault()?.Reference?.Id;
                }
                if (string.IsNullOrEmpty(schemaReferenceId))
                {
                    continue;
                }

                var schema = context.SchemaRepository.Schemas[schemaReferenceId];
                if (schema.Enum == null || schema.Enum.Count == 0)
                {
                    continue;
                }

                parameter.Description += "<p>Variants:</p>";
                int cutStart = schema.Description.IndexOf("<ul>");
                int cutEnd = schema.Description.IndexOf("</ul>") + 5;

                parameter.Description += schema.Description
                    .Substring(cutStart, cutEnd - cutStart);
            }
        }
    }
}
