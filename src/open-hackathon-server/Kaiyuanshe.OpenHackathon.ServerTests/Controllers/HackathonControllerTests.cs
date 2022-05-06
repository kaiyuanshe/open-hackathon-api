using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    [TestFixture]
    public class HackathonControllerTests
    {
        #region CheckNameAvailability
        [TestCase("")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase("@")]
        [TestCase("#")]
        [TestCase("%")]
        [TestCase("-")]
        [TestCase("_")]
        [TestCase("=")]
        [TestCase(" ")]
        public async Task CheckNameAvailability_Invalid(string name)
        {
            var parameter = new NameAvailability { name = name };
            CancellationToken cancellationToken = CancellationToken.None;

            var controller = new HackathonController();
            var result = await controller.CheckNameAvailability(parameter, cancellationToken);
            NameAvailability resp = (NameAvailability)(result);
            Assert.AreEqual(name, resp.name);
            Assert.IsFalse(resp.nameAvailable);
            Assert.AreEqual("Invalid", resp.reason);
            Assert.AreEqual(Resources.Hackathon_Name_Invalid, resp.message);
        }

        [Test]
        public async Task CheckNameAvailability_AlreadyExists()
        {
            var parameter = new NameAvailability { name = "Foo" };
            CancellationToken cancellationToken = CancellationToken.None;
            HackathonEntity entity = new HackathonEntity();

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("foo", cancellationToken))
                .ReturnsAsync(entity);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
            };
            var result = await controller.CheckNameAvailability(parameter, cancellationToken);

            NameAvailability resp = (NameAvailability)(result);
            Assert.AreEqual("Foo", resp.name);
            Assert.IsFalse(resp.nameAvailable);
            Assert.AreEqual("AlreadyExists", resp.reason);
            Assert.AreEqual(Resources.Hackathon_Name_Taken, resp.message);
        }

        [Test]
        public async Task CheckNameAvailability_OK()
        {
            var parameter = new NameAvailability { name = "Foo" };
            CancellationToken cancellationToken = CancellationToken.None;
            HackathonEntity entity = null;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("foo", cancellationToken))
                .ReturnsAsync(entity);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
            };
            var result = await controller.CheckNameAvailability(parameter, cancellationToken);

            NameAvailability resp = (NameAvailability)(result);
            Assert.AreEqual("Foo", resp.name);
            Assert.IsTrue(resp.nameAvailable);
        }

        #endregion

        #region CreateOrUpdate
        [Test]
        public async Task CreateOrUpdateTest_CreateTooMany()
        {
            var hack = new Hackathon();

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(default(HackathonEntity));
            hackathonManagement.Setup(p => p.CanCreateHackathonAsync(It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(false);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
            };
            var result = await controller.CreateOrUpdate("Hack", hack, default);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_CreateTooMany);
        }

        [Test]
        public async Task CreateOrUpdateTest_Create()
        {
            var hack = new Hackathon { displayName = "dp" };
            var inserted = new HackathonEntity
            {
                PartitionKey = "test2",
                AutoApprove = true
            };
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(default(HackathonEntity));
            moqs.HackathonManagement.Setup(p => p.CreateHackathonAsync(hack, default)).ReturnsAsync(inserted);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(inserted, null, default)).ReturnsAsync(role);
            moqs.HackathonManagement.Setup(p => p.CanCreateHackathonAsync(It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(true);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.createHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.createHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", hack, default);

            // verify
            moqs.VerifyAll();
            Hackathon resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("test2", resp.name);
            Assert.IsTrue(resp.autoApprove);
            Assert.IsTrue(resp.roles.isAdmin);
        }

        [Test]
        public async Task CreateOrUpdateTest_UpdateForbidden()
        {
            var hack = new Hackathon();
            var entity = new HackathonEntity();
            var name = "test1";
            var authResult = AuthorizationResult.Failed();
            CancellationToken cancellationToken = CancellationToken.None;

            var hackManagerMock = new Mock<IHackathonManagement>();
            hackManagerMock.Setup(p => p.GetHackathonEntityByNameAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(entity);
            var authorizationServiceMock = new Mock<IAuthorizationService>();
            authorizationServiceMock.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new HackathonController
            {
                HackathonManagement = hackManagerMock.Object,
                AuthorizationService = authorizationServiceMock.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.CreateOrUpdate(name, hack, cancellationToken);

            Mock.VerifyAll(hackManagerMock, authorizationServiceMock);
            hackManagerMock.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task CreateOrUpdateTest_UpdateSucceeded()
        {
            var hack = new Hackathon();
            var entity = new HackathonEntity
            {
                PartitionKey = "test2",
                AutoApprove = true
            };
            var authResult = AuthorizationResult.Success();
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(p => p.UpdateHackathonAsync(hack, default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
                           .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", hack, default);

            // verify
            moqs.VerifyAll();

            Hackathon resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("test2", resp.name);
            Assert.AreEqual("test2", resp.name);
            Assert.IsTrue(resp.autoApprove);
            Assert.IsTrue(resp.roles.isAdmin);
        }

        #endregion

        #region Update
        [Test]
        public async Task UpdateTest_NotFound()
        {
            string name = "Foo";
            HackathonEntity entity = null;
            Hackathon parameter = new Hackathon();

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Update(name, parameter, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name.ToLower()));
        }

        [Test]
        public async Task UpdateTest_ReadOnly()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { ReadOnly = true };
            Hackathon parameter = new Hackathon();

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Update(name, parameter, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_ReadOnly);
        }

        [Test]
        public async Task UpdateTest_AccessDenied()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            Hackathon parameter = new Hackathon();
            var authResult = AuthorizationResult.Failed();

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
              .ReturnsAsync(authResult);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Update(name, parameter, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task UpdateTest_Updated()
        {
            HackathonEntity entity = new HackathonEntity { PartitionKey = "test2", DisplayName = "displayname" };
            Hackathon parameter = new Hackathon();
            var authResult = AuthorizationResult.Success();
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(p => p.UpdateHackathonAsync(parameter, default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
                           .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", parameter, default);

            // verify
            moqs.VerifyAll();

            Hackathon resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("hack", parameter.name);
            Assert.AreEqual("displayname", resp.displayName);
            Assert.IsTrue(resp.roles.isAdmin);
        }

        #endregion

        #region Get
        [Test]
        public async Task GetTest_NotFound()
        {
            string name = "Foo";
            HackathonEntity entity = null;
            CancellationToken cancellationToken = CancellationToken.None;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Get(name, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name));
        }

        [Test]
        public async Task GetTest_NotOnlineAnonymous()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Detail = "detail" };
            CancellationToken cancellationToken = CancellationToken.None;
            HackathonRoles role = null;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);
            hackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), cancellationToken))
              .ReturnsAsync(role);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Get(name, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name));
        }

        [Test]
        public async Task GetTest_NotOnlineNotAdmin()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Detail = "detail" };
            CancellationToken cancellationToken = CancellationToken.None;
            HackathonRoles role = new HackathonRoles { isAdmin = false };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);
            hackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), cancellationToken))
              .ReturnsAsync(role);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Get(name, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name));
        }

        [Test]
        public async Task GetTest_OK()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Detail = "detail", ReadOnly = true };
            CancellationToken cancellationToken = CancellationToken.None;
            var role = new HackathonRoles { isAdmin = true };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);
            hackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), cancellationToken))
              .ReturnsAsync(role);

            var controller = new HackathonController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.Get(name, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            Assert.IsTrue(result is OkObjectResult);
            Hackathon hackathon = ((OkObjectResult)result).Value as Hackathon;
            Assert.IsNotNull(hackathon);
            Assert.AreEqual("detail", hackathon.detail);
            Assert.IsTrue(hackathon.roles.isAdmin);
        }

        #endregion

        #region ListHackathon
        private static IEnumerable ListHackathonTestData()
        {
            // arg0: pagination
            // arg1: search
            // arg2: userId
            // arg3: order by
            // arg4: listType
            // arg5: next pagination
            // arg6: expected nextlink

            // no pagination, no filter, no top
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    "search",
                    null,
                    HackathonOrderBy.updatedAt,
                    HackathonListType.online,
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10 },
                    "search",
                    null,
                    HackathonOrderBy.updatedAt,
                    HackathonListType.admin,
                    new Pagination { np = "np", nr = "nr" },
                    "&top=10&search=search&orderby=updatedAt&listType=admin&np=np&nr=nr"
                );
            yield return new TestCaseData(
                    new Pagination { top = 10 },
                    "search",
                    null,
                    HackathonOrderBy.updatedAt,
                    HackathonListType.online,
                    new Pagination { np = "np", nr = "nr" },
                    "&top=10&search=search&orderby=updatedAt&listType=online&np=np&nr=nr"
                );

            // with userId, pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10 },
                    "search",
                    "uid2",
                    HackathonOrderBy.updatedAt,
                    HackathonListType.admin,
                    new Pagination { np = "np", nr = "nr" },
                    "&top=10&search=search&orderby=updatedAt&listType=admin&np=np&nr=nr"
                );
        }

        [Test, TestCaseSource(nameof(ListHackathonTestData))]
        public async Task ListHackathon(
            Pagination pagination,
            string search,
            string userId,
            HackathonOrderBy? orderBy,
            HackathonListType? listType,
            Pagination next,
            string expectedLink)
        {
            // input
            var hackathons = new List<HackathonEntity>
            {
                new HackathonEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                }
            };
            var hackWithRoles = new List<Tuple<HackathonEntity, HackathonRoles>>()
            {
                Tuple.Create(hackathons.First(), new HackathonRoles{})
            };

            // mock and capture
            var mockContext = new MockControllerContext();
            HackathonQueryOptions optionsCaptured = null;
            mockContext.HackathonManagement.Setup(p => p.ListPaginatedHackathonsAsync(It.IsAny<HackathonQueryOptions>(), default))
                .Callback<HackathonQueryOptions, CancellationToken>((o, t) =>
                 {
                     optionsCaptured = o;
                     optionsCaptured.NextPage = next;
                 })
                .ReturnsAsync(hackathons);
            mockContext.HackathonManagement.Setup(h => h.ListHackathonRolesAsync(hackathons, It.IsAny<ClaimsPrincipal>(), default))
                .ReturnsAsync(hackWithRoles);
            mockContext.HackathonAdminManagement.Setup(a => a.IsPlatformAdmin(It.IsAny<string>(), default)).ReturnsAsync(true);

            // run
            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.ListHackathon(pagination, search, userId, orderBy, listType, default);

            // verify
            mockContext.VerifyAll();

            var list = AssertHelper.AssertOKResult<HackathonList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].name);
            Assert.AreEqual(pagination.top, optionsCaptured.Pagination?.top);
            Assert.AreEqual(orderBy, optionsCaptured.OrderBy);
            Assert.AreEqual(userId ?? string.Empty, optionsCaptured.UserId);
            Assert.AreEqual(true, optionsCaptured.IsPlatformAdmin);
            Assert.AreEqual(listType, optionsCaptured.ListType);
            Assert.AreEqual(pagination.np, optionsCaptured.Pagination?.np);
            Assert.AreEqual(pagination.nr, optionsCaptured.Pagination?.nr);
        }
        #endregion

        #region Delete
        [Test]
        public async Task DeleteTest_NotExist()
        {
            string name = "Foo";
            HackathonEntity entity = null;

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Delete(name, default);

            mockContext.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }

        [Test]
        public async Task DeleteTest_ReadOnly()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { ReadOnly = true };

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Delete(name, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_ReadOnly);
        }

        [Test]
        public async Task DeleteTest_NotAdmin()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            var authResult = AuthorizationResult.Failed();

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            mockContext.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Delete(name, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task DeleteTest_HasAwardAssignment()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var assignmentCount = 1;

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            mockContext.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            mockContext.AwardManagement.Setup(a => a.GetAssignmentCountAsync("foo", null, default)).ReturnsAsync(assignmentCount);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.Delete(name, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_HasAwardAssignment);
        }

        [Test]
        public async Task DeleteTest_Success()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var assignmentCount = 0;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(a => a.GetAssignmentCountAsync("foo", null, default)).ReturnsAsync(assignmentCount);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonStatusAsync(entity, HackathonStatus.offline, default));
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.deleteHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.deleteHackathon, It.IsAny<object>(), null, default));

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Delete(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion

        #region RequestPublish
        [Test]
        public async Task RequestPublish_AlreadyOnline()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Status = HackathonStatus.online };
            var authResult = AuthorizationResult.Success();

            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.RequestPublish(name, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, string.Format(Resources.Hackathon_AlreadyOnline, name));
        }

        [TestCase(HackathonStatus.planning)]
        [TestCase(HackathonStatus.pendingApproval)]
        public async Task RequestPublish_Succeeded(HackathonStatus status)
        {
            string name = "Hack";
            HackathonEntity entity = new HackathonEntity
            {
                PartitionKey = "pk",
                Status = status,
                DisplayName = "dpn"
            };
            var authResult = AuthorizationResult.Success();
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonStatusAsync(entity, HackathonStatus.pendingApproval, default))
                .Callback<HackathonEntity, HackathonStatus, CancellationToken>((e, s, c) =>
                {
                    e.Status = s;
                })
                .ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, null, default)).ReturnsAsync(role);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
               .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("pk", It.IsAny<string>(), ActivityLogType.publishHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "pk", It.IsAny<string>(), ActivityLogType.publishHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.RequestPublish(name, default);

            // verify
            moqs.VerifyAll();

            var resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual(HackathonStatus.pendingApproval, resp.status);
        }
        #endregion

        #region Publish
        [TestCase(HackathonStatus.planning)]
        [TestCase(HackathonStatus.pendingApproval)]
        [TestCase(HackathonStatus.offline)]
        public async Task Publish_Succeeded(HackathonStatus status)
        {
            string name = "Hack";
            HackathonEntity entity = new HackathonEntity { PartitionKey = "foo", Status = status, DisplayName = "dpn", };
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonStatusAsync(entity, HackathonStatus.online, default))
                .Callback<HackathonEntity, HackathonStatus, CancellationToken>((e, s, c) =>
                {
                    e.Status = s;
                }).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, null, default)).ReturnsAsync(role);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.approveHackahton, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.approveHackahton, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Publish(name, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual(HackathonStatus.online, resp.status);
        }
        #endregion

        #region UpdateReadonly
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateReadonly(bool readOnly)
        {
            HackathonEntity entity = new HackathonEntity { DisplayName = "dpn" };
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            mockContext.HackathonManagement.Setup(m => m.UpdateHackathonReadOnlyAsync(entity, readOnly, default))
                .Callback<HackathonEntity, bool, CancellationToken>((h, r, c) =>
                {
                    h.ReadOnly = r;
                })
                .ReturnsAsync(entity);
            mockContext.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, null, default)).ReturnsAsync(role);
            mockContext.ActivityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                && a.ActivityLogType == ActivityLogType.archiveHackathon.ToString()
                && a.Message == "dpn"), default));

            // test
            var controller = new HackathonController();
            mockContext.SetupController(controller);
            var result = await controller.UpdateReadonly("Hack", readOnly, default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement, mockContext.ActivityLogManagement);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ActivityLogManagement.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual(readOnly, resp.readOnly);
        }
        #endregion
    }
}
