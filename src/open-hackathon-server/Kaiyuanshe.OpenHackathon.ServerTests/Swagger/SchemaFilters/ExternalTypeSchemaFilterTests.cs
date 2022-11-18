using Kaiyuanshe.OpenHackathon.Server.Swagger.SchemaFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Swagger.SchemaFilters
{
    internal class ExternalTypeSchemaFilterTests : SwaggerTest
    {
        [Test]
        public void Apply()
        {
            var schemaRepository = new SchemaRepository();
            var schemaGenerator = GetSchemaGenerator(
                configureGenerator: options =>
                {
                    options.SchemaFilters.Add(new ExternalTypeSchemaFilter());
                });
            schemaGenerator.GenerateSchema(typeof(ProblemDetails), schemaRepository);

            // FakeModel schema
            OpenApiSchema schema = AssertTypeRegistered<ProblemDetails>(schemaRepository);
            Assert.IsTrue(schema.Description.Contains("A machine-readable format for specifying errors in HTTP API responses"));
            Assert.IsTrue(schema.Description.Contains("See more"));
            Assert.IsTrue(schema.Description.Contains("microsoft.aspnetcore.mvc.problemdetails"));
        }
    }
}
