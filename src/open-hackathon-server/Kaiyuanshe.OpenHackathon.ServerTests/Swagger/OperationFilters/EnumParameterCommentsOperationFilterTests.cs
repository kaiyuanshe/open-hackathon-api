using Kaiyuanshe.OpenHackathon.Server.Swagger.DocumentFilters;
using Kaiyuanshe.OpenHackathon.Server.Swagger.SchemaFilters;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Swagger.OperationFilters
{
    internal class EnumParameterCommentsOperationFilterTests : SwaggerTest
    {
        [Test]
        public void ApplyTest()
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
                    serializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            schemaGenerator.GenerateSchema(typeof(FakeEnum), schemaRepository);
            var generator = Generate(
                    apiDescriptions: new[]
                    {
                        ApiDescriptionFactory.Create<FakeController>(
                            c => nameof(c.ActionWithEnumParameter),
                            groupName: "v1",
                            httpMethod: "GET",
                            relativePath: "resource",
                            parameterDescriptions: new List<ApiParameterDescription>
                            {
                                new ApiParameterDescription
                                {
                                    Name = "param1",
                                    Source = BindingSource.Query,
                                    Type = typeof(FakeEnum),
                                },
                            }
                        ),
                    },
                    configure: (options) =>
                    {
                        options.OperationFilters.Add(new EnumParameterCommentsOperationFilter());
                    },
                    schemaGenerator: schemaGenerator
            );

            var document = generator.GetSwagger("v1");

            var get = document.Paths["/resource"].Operations[OperationType.Get];
            Assert.AreEqual(1, get.Parameters.Count);
            Assert.AreEqual("param1", get.Parameters[0].Name);
            Assert.IsTrue(get.Parameters[0].Description.Contains("Variant"));
            Assert.IsTrue(get.Parameters[0].Description.Contains("Summary One"));
            Assert.IsTrue(get.Parameters[0].Description.Contains("Summary Two"));
            Assert.IsTrue(get.Parameters[0].Description.Contains("Summary Three"));
        }
    }
}
