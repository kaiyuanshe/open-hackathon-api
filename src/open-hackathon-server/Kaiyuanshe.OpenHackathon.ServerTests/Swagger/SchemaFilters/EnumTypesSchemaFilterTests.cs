using Kaiyuanshe.OpenHackathon.Server.Swagger.SchemaFilters;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Swagger.SchemaFilters
{
    internal class EnumTypesSchemaFilterTests : SwaggerTest
    {
        [Test]
        public void EnumSchemaFilterTest()
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "Swagger", "XmlComments.xml");

            var schemaRepository = new SchemaRepository();
            var schemaGenerator = GetSchemaGenerator(
                configureGenerator: options =>
                {
                    options.SchemaFilters.Add(new EnumTypesSchemaFilter(xmlPath));
                },
                configureSerializer: serializerOptions =>
                {
                    serializerOptions.Converters.Add(new JsonStringEnumConverter
                    {
                    });
                });
            schemaGenerator.GenerateSchema(typeof(FakeEnum), schemaRepository);

            // FakeModel schema
            OpenApiSchema schema = AssertTypeRegistered<FakeEnum>(schemaRepository);
            Assert.IsTrue(schema.Description.Contains("<p>Members:</p><ul>"));
            Assert.IsTrue(schema.Description.Contains("Summary One"));
            Assert.IsTrue(schema.Description.Contains("Summary Two"));
            Assert.IsTrue(schema.Description.Contains("Summary Three"));
        }
    }
}
