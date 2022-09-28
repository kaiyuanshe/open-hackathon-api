using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    internal class ReportControllerTests
    {
        [Test]
        public async Task GetReport_HackNotFound()
        {
            HackathonEntity? hackathon = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            var controller = new ReportController();
            moqs.SetupController(controller);
            var result = await controller.GetReport("Hack", ReportType.enrollments, null, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, "Hack"));
        }

        [Test]
        public async Task GetReport_ValidateHeaderFailed()
        {
            HackathonEntity? hackathon = new HackathonEntity();
            AuthorizationResult auth = AuthorizationResult.Failed();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(auth);

            var controller = new ReportController();
            moqs.SetupController(controller);
            var result = await controller.GetReport("Hack", ReportType.enrollments, null, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task GetReport_ValidateTokenFailed()
        {
            HackathonEntity? hackathon = new HackathonEntity();
            var claims = new List<Claim> { ClaimsHelper.UserId("uid") };
            AuthorizationResult auth = AuthorizationResult.Failed();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.UserManagement.Setup(u => u.GetUserBasicClaimsAsync("token", default)).ReturnsAsync(claims);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(
                    It.Is<ClaimsPrincipal>(cp => cp.Claims.Count() == 1 && cp.Claims.First().Value == "uid"),
                    hackathon,
                    AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(auth);

            var controller = new ReportController();
            moqs.SetupController(controller);
            var result = await controller.GetReport("Hack", ReportType.enrollments, "token", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [TestCase(ReportType.enrollments)]
        public async Task GetReport_NoReport(ReportType reportType)
        {
            HackathonEntity? hackathon = new HackathonEntity();
            var claims = new List<Claim> { ClaimsHelper.UserId("uid") };
            AuthorizationResult auth = AuthorizationResult.Success();
            byte[]? reportContent = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.UserManagement.Setup(u => u.GetUserBasicClaimsAsync("token", default)).ReturnsAsync(claims);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(
                    It.Is<ClaimsPrincipal>(cp => cp.Claims.Count() == 1 && cp.Claims.First().Value == "uid"),
                    hackathon,
                    AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(auth);
            moqs.FileManagement.Setup(f => f.DownloadReport("hack", reportType, default)).ReturnsAsync(reportContent);

            var controller = new ReportController();
            moqs.SetupController(controller);
            var result = await controller.GetReport("Hack", reportType, "token", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.HackathonReport_NotFound);
        }

        [TestCase(ReportType.enrollments)]
        public async Task GetReport_Downloaded(ReportType reportType)
        {
            HackathonEntity? hackathon = new HackathonEntity { PartitionKey = "hack" };
            var claims = new List<Claim> { ClaimsHelper.UserId("uid") };
            AuthorizationResult auth = AuthorizationResult.Success();
            byte[]? reportContent = Encoding.UTF8.GetBytes("reportContent");

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.UserManagement.Setup(u => u.GetUserBasicClaimsAsync("token", default)).ReturnsAsync(claims);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(
                    It.Is<ClaimsPrincipal>(cp => cp.Claims.Count() == 1 && cp.Claims.First().Value == "uid"),
                    hackathon,
                    AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(auth);
            moqs.FileManagement.Setup(f => f.DownloadReport("hack", reportType, default)).ReturnsAsync(reportContent);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "uid", ActivityLogType.downloadReport, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("uid", "hack", "uid", ActivityLogType.downloadReport, It.IsAny<object>(), null, default));

            var controller = new ReportController();
            moqs.SetupController(controller);
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            var httpHeaders = new Mock<IHeaderDictionary>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);
            httpResponse.SetupGet(h => h.Headers).Returns(httpHeaders.Object);
            httpHeaders.Setup(h => h.Add(HeaderNames.ContentDisposition, $"attachment;filename=hack-{reportType}.csv"));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

            var result = await controller.GetReport("Hack", reportType, "token", default);

            moqs.VerifyAll();
            Mock.Verify(httpContext, httpResponse, httpHeaders);
            Assert.IsTrue(result is FileContentResult);
            FileContentResult fileContent = (FileContentResult)result;
            Assert.AreEqual("text/csv", fileContent.ContentType);
            Assert.AreEqual($"hack-{reportType}.csv", fileContent.FileDownloadName);
            Assert.AreEqual(reportContent, fileContent.FileContents);
        }
    }
}
