using Kaiyuanshe.OpenHackathon.Server.Swagger.OperationFilters;
using Microsoft.OpenApi.Models;
using NUnit.Framework;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Swagger.OperationFilters
{
    [TestFixture]
    public class UnauthorizedResponseOperationFilterTests : SwaggerTest
    {
        [Test]
        public void UnauthorizedResponseOperationFilterTest()
        {
            var generator = Generate(
                   apiDescriptions: new[]
                   {
                       ApiDescriptionFactory.Create<FakeController>(
                        c => nameof(c.ActionWithTokenRequired),
                        groupName: "v1",
                        httpMethod: "GET",
                        relativePath: "resource"),
                   },
                   configure: (options) =>
                   {
                       options.OperationFilters.Add(new UnauthorizedResponseOperationFilter());
                   }
            );

            var document = generator.GetSwagger("v1");

            var get = document.Paths["/resource"].Operations[OperationType.Get];
            Assert.AreEqual(2, get.Responses.Count);
            Assert.IsTrue(get.Responses.ContainsKey("200"));
            Assert.IsTrue(get.Responses.ContainsKey("401"));
        }
    }
}
