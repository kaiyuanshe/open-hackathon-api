using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.XPath;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Swagger
{
    public class SwaggerTest
    {
        protected SwaggerGenerator Generate(IEnumerable<ApiDescription> apiDescriptions, Action<SwaggerGeneratorOptions>? configure = null)
        {
            var options = DefaultOptions;
            configure?.Invoke(options);
            return new SwaggerGenerator(
                options,
                new FakeApiDescriptionGroupCollectionProvider(apiDescriptions),
                new SchemaGenerator(new SchemaGeneratorOptions(), new JsonSerializerDataContractResolver(new JsonSerializerOptions()))
            );
        }

        protected SchemaGenerator GetSchemaGenerator(Action<SchemaGeneratorOptions>? configureGenerator = null, Action<JsonSerializerOptions>? configureSerializer = null)
        {
            var generatorOptions = new SchemaGeneratorOptions();
            configureGenerator?.Invoke(generatorOptions);

            var serializerOptions = new JsonSerializerOptions();
            configureSerializer?.Invoke(serializerOptions);

            return new SchemaGenerator(generatorOptions, new JsonSerializerDataContractResolver(serializerOptions));
        }

        private static readonly SwaggerGeneratorOptions DefaultOptions = new SwaggerGeneratorOptions
        {
            SwaggerDocs = new Dictionary<string, OpenApiInfo>
            {
                ["v1"] = new OpenApiInfo { Version = "V1", Title = "Test API" },
            },
            DocumentFilters = new List<IDocumentFilter>(),
            OperationFilters = new List<IOperationFilter>(),
        };

        protected XPathDocument XmlComment()
        {
            var xmlComments = GetType().Assembly.GetManifestResourceStream("Kaiyuanshe.OpenHackathon.ServerTests.Swagger.XmlComments.xml");
            Debug.Assert(xmlComments != null);
            return new XPathDocument(xmlComments);
        }

        #region Assert
        protected OpenApiSchema AssertTypeRegistered<T>(SchemaRepository schemaRepository)
        {
            Assert.IsTrue(schemaRepository.TryLookupByType(typeof(T), out OpenApiSchema schema));
            return schemaRepository.Schemas[schema.Reference.Id];
        }
        #endregion
    }

    public static class IOpenApiExtensionHelper
    {
        public static string? GetStringValue(this IOpenApiExtension openApiExtension)
        {
            return GetValue<string>(openApiExtension);
        }

        public static bool GetBoolValue(this IOpenApiExtension openApiExtension)
        {
            return GetValue<bool>(openApiExtension);
        }

        public static int GetIntValue(this IOpenApiExtension openApiExtension)
        {
            return GetValue<int>(openApiExtension);
        }

        public static T? GetValue<T>(this IOpenApiExtension openApiExtension)
        {
            if (openApiExtension is OpenApiPrimitive<T> primitive)
            {
                return primitive.Value;
            }

            return default;
        }

        public static OpenApiArray? AsArray(this IOpenApiExtension openApiExtension)
        {
            if (openApiExtension is OpenApiArray array)
            {
                return array;
            }

            return default;
        }
    }
}
