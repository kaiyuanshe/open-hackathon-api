using Kaiyuanshe.OpenHackathon.Server.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Auth
{
    class TrustedAppHandlerTests
    {
        [Test]
        public async Task HandleRequirementAsync_NoHeader()
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(p => p["Guacamole:TrustedApps"]).Returns("a,b;c");

            var headers = new HeaderDictionary();
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Headers).Returns(headers);

            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Request).Returns(request.Object);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.SetupGet(a => a.HttpContext).Returns(context.Object);

            var logger = new Mock<ILogger<TrustedAppHandler>>();

            var authContext = new AuthorizationHandlerContext(new List<IAuthorizationRequirement>
            {
                new TrustedAppRequirement()
            }, null, null);
            var handler = new TrustedAppHandler(config.Object, accessor.Object, logger.Object);
            await handler.HandleAsync(authContext);

            Mock.VerifyAll(config, request, context, accessor);
            config.VerifyNoOtherCalls();
            request.VerifyNoOtherCalls();
            context.VerifyNoOtherCalls();
            accessor.VerifyNoOtherCalls();

            Assert.IsFalse(authContext.HasSucceeded);
        }

        [Test]
        public async Task HandleRequirementAsync_NotAllowed()
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(p => p["Guacamole:TrustedApps"]).Returns("aa,bb;cc");

            var headers = new HeaderDictionary();
            headers.Add("x-openhackathon-app-id", "dd");
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Headers).Returns(headers);

            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Request).Returns(request.Object);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.SetupGet(a => a.HttpContext).Returns(context.Object);

            var logger = new Mock<ILogger<TrustedAppHandler>>();

            var authContext = new AuthorizationHandlerContext(new List<IAuthorizationRequirement>
            {
                new TrustedAppRequirement()
            }, null, null);
            var handler = new TrustedAppHandler(config.Object, accessor.Object, logger.Object);
            await handler.HandleAsync(authContext);

            Mock.VerifyAll(config, request, context, accessor);
            config.VerifyNoOtherCalls();
            request.VerifyNoOtherCalls();
            context.VerifyNoOtherCalls();
            accessor.VerifyNoOtherCalls();

            Assert.IsFalse(authContext.HasSucceeded);
        }

        [TestCase("aa")]
        [TestCase("bb")]
        [TestCase("cc")]
        public async Task HandleRequirementAsync_Success(string appId)
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(p => p["Guacamole:TrustedApps"]).Returns("aa,bb;cc");

            var headers = new HeaderDictionary();
            headers.Add("x-openhackathon-app-id", appId);
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Headers).Returns(headers);

            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Request).Returns(request.Object);

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.SetupGet(a => a.HttpContext).Returns(context.Object);

            var logger = new Mock<ILogger<TrustedAppHandler>>();

            var authContext = new AuthorizationHandlerContext(new List<IAuthorizationRequirement>
            {
                new TrustedAppRequirement()
            }, null, null);
            var handler = new TrustedAppHandler(config.Object, accessor.Object, logger.Object);
            await handler.HandleAsync(authContext);

            Mock.VerifyAll(config, request, context, accessor);
            config.VerifyNoOtherCalls();
            request.VerifyNoOtherCalls();
            context.VerifyNoOtherCalls();
            accessor.VerifyNoOtherCalls();

            Assert.IsTrue(authContext.HasSucceeded);
        }
    }
}
