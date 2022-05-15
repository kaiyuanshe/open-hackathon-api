using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    [TestFixture]
    public class EnrollmentControllerTests
    {
        #region Enroll
        [Test]
        public async Task EnrollTest_HackNotFound()
        {
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = null;
            CancellationToken cancellationToken = CancellationToken.None;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);


            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Enroll(hackathonName, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test]
        public async Task EnrollTest_NotOnline()
        {
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                Status = HackathonStatus.pendingApproval
            };
            CancellationToken cancellationToken = CancellationToken.None;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Enroll(hackathonName, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_NotOnline);
        }

        [Test]
        public async Task EnrollTest_PreConditionFailed1()
        {
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
            };
            CancellationToken cancellationToken = CancellationToken.None;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);


            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Enroll(hackathonName, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 412);
        }

        [Test]
        public async Task EnrollTest_PreConditionFailed2()
        {
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(-1),
                Status = HackathonStatus.online,
            };
            CancellationToken cancellationToken = CancellationToken.None;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);


            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Enroll(hackathonName, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 412);
        }

        [Test]
        public async Task EnrollTest_PreConditionFailed3()
        {
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
                Enrollment = 5,
                MaxEnrollment = 4,
            };
            CancellationToken cancellationToken = CancellationToken.None;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);


            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Enroll(hackathonName, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 412, Resources.Enrollment_Full);
        }

        [Test]
        public async Task EnrollTest_Insert()
        {
            // data
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                PartitionKey = "test2",
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
                MaxEnrollment = 0,
            };
            Enrollment request = new Enrollment();
            EnrollmentEntity enrollment = new EnrollmentEntity { PartitionKey = "pk" };
            UserInfo userInfo = new UserInfo();

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "", default)).ReturnsAsync(default(EnrollmentEntity));
            moqs.EnrollmentManagement.Setup(p => p.CreateEnrollmentAsync(hackathonEntity, request, default)).ReturnsAsync(enrollment);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default)).ReturnsAsync(userInfo);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.createEnrollment, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.createEnrollment, It.IsAny<object>(), null, default));

            // test
            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Enroll(hackathonName, request, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Enrollment>(result);
            Assert.AreEqual("pk", enrollment.HackathonName);
        }

        [Test]
        public async Task EnrollTest_Update()
        {
            string hackathonName = "Hack";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                PartitionKey = "test2",
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
                MaxEnrollment = 0,
            };
            Enrollment request = new Enrollment { };
            EnrollmentEntity enrollment = new EnrollmentEntity { PartitionKey = "pk", RowKey = "rk" };
            UserInfo userInfo = new UserInfo();

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "", default)).ReturnsAsync(enrollment);
            moqs.EnrollmentManagement.Setup(p => p.UpdateEnrollmentAsync(enrollment, request, default)).ReturnsAsync(enrollment);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default)).ReturnsAsync(userInfo);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), nameof(Resources.ActivityLog_User2_updateEnrollment), default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("rk", "test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), null, default));

            // test
            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Enroll(hackathonName, request, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Enrollment>(result);
            Assert.AreEqual("pk", enrollment.HackathonName);
        }
        #endregion

        #region Update
        [Test]
        public async Task Update_Forbidden()
        {
            string hackathonName = "Hack";
            string userId = "uid";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
            };
            Enrollment request = new Enrollment { };
            EnrollmentEntity enrollment = new EnrollmentEntity { };
            var authResult = AuthorizationResult.Failed();

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                AuthorizationService = authorizationService.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.Update(hackathonName, userId, request, default);

            Mock.VerifyAll(hackathonManagement, authorizationService);
            hackathonManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task Update_TooManyExtensions()
        {
            string hackathonName = "Hack";
            string userId = "";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
            };
            Enrollment request = new Enrollment { extensions = new Extension[11] };
            for (int i = 0; i < 11; i++)
            {
                request.extensions[i] = new Extension { name = i.ToString(), value = i.ToString() };
            }
            EnrollmentEntity enrollment = new EnrollmentEntity { };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "", default)).ReturnsAsync(enrollment);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Update(hackathonName, userId, request, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 400, string.Format(Resources.Enrollment_TooManyExtensions, 10));
        }

        [Test]
        public async Task Update_Succeeded()
        {
            string hackathonName = "Hack";
            string userId = "uid";
            HackathonEntity hackathonEntity = new HackathonEntity
            {
                PartitionKey = "test2",
                EnrollmentStartedAt = DateTime.UtcNow.AddDays(-1),
                EnrollmentEndedAt = DateTime.UtcNow.AddDays(1),
                Status = HackathonStatus.online,
            };
            Enrollment request = new Enrollment { extensions = new Extension[10] };
            for (int i = 0; i < 10; i++)
            {
                request.extensions[i] = new Extension { name = i.ToString(), value = i.ToString() };
            }
            EnrollmentEntity enrollment = new EnrollmentEntity { PartitionKey = "pk", RowKey = "uid" };
            var authResult = AuthorizationResult.Success();
            UserInfo userInfo = new UserInfo();

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "uid", default)).ReturnsAsync(enrollment);
            moqs.EnrollmentManagement.Setup(p => p.UpdateEnrollmentAsync(enrollment, request, default)).ReturnsAsync(enrollment);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default)).ReturnsAsync(userInfo);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), nameof(Resources.ActivityLog_User2_updateEnrollment), default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("uid", "test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), null, default));

            // test
            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Update(hackathonName, userId, request, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Enrollment>(result);
            Assert.AreEqual("uid", resp.userId);
        }
        #endregion

        #region Get Enrollment
        [Test]
        public async Task GetTest_NotFound()
        {
            string hackathonName = "hack";
            EnrollmentEntity enrollment = null;

            var moqs = new Moqs();
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", string.Empty, default)).ReturnsAsync(enrollment);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Get(hackathonName, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test]
        public async Task GetTest_Ok()
        {
            string hackathonName = "hack";
            EnrollmentEntity enrollment = new EnrollmentEntity { Status = EnrollmentStatus.pendingApproval };
            UserInfo userInfo = new UserInfo();

            var moqs = new Moqs();
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", string.Empty, default)).ReturnsAsync(enrollment);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default)).ReturnsAsync(userInfo);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Get(hackathonName, default);

            moqs.VerifyAll();
            Assert.IsTrue(result is OkObjectResult);
            OkObjectResult objectResult = (OkObjectResult)result;
            Enrollment en = (Enrollment)objectResult.Value;
            Assert.IsNotNull(en);
            Assert.AreEqual(EnrollmentStatus.pendingApproval, enrollment.Status);
        }
        #endregion

        #region Approve/Reject
        private static IEnumerable ListStatus()
        {
            yield return EnrollmentStatus.approved;
            yield return EnrollmentStatus.rejected;
        }

        private static Func<string, string, Enrollment, CancellationToken, Task<object>> GetTargetMethod(EnrollmentController controller, EnrollmentStatus status)
        {
            if (status == EnrollmentStatus.approved)
                return controller.Approve;

            if (status == EnrollmentStatus.rejected)
                return controller.Reject;

            return null;
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_HackNotFound(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = null;
            CancellationToken cancellationToken = default;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_Forbidden(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Failed();
            CancellationToken cancellationToken = default;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);

            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
                AuthorizationService = authorizationService.Object,
            };
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement, authorizationService);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 403);
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_EnrollmentNotFound(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            EnrollmentEntity participant = null;
            CancellationToken cancellationToken = default;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", "uid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);

            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
                AuthorizationService = authorizationService.Object,
            };
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement, authorizationService);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_Succeeded(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            EnrollmentEntity enrollment = new EnrollmentEntity
            {
                PartitionKey = "pk",
                Status = EnrollmentStatus.pendingApproval,
            };
            UserInfo userInfo = new UserInfo();

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            mockContext.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "uid", default)).ReturnsAsync(enrollment);
            mockContext.EnrollmentManagement.Setup(p => p.UpdateEnrollmentStatusAsync(hackathonEntity, It.IsAny<EnrollmentEntity>(), It.IsAny<EnrollmentStatus>(), default))
                            .Callback<HackathonEntity, EnrollmentEntity, EnrollmentStatus, CancellationToken>((h, p, e, c) =>
                            {
                                p.Status = e;
                            })
                            .ReturnsAsync(enrollment);
            mockContext.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default)).ReturnsAsync(userInfo);
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            var activityLogType = status == EnrollmentStatus.approved ? ActivityLogType.approveEnrollment : ActivityLogType.rejectEnrollment;
            mockContext.ActivityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "pk"
                && a.ActivityLogType == activityLogType.ToString()), default));

            // test
            var controller = new EnrollmentController();
            mockContext.SetupController(controller);
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, null, default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.UserManagement,
                mockContext.EnrollmentManagement,
                mockContext.ActivityLogManagement,
                mockContext.AuthorizationService);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.UserManagement.VerifyNoOtherCalls();
            mockContext.EnrollmentManagement.VerifyNoOtherCalls();
            mockContext.ActivityLogManagement.VerifyNoOtherCalls();
            mockContext.AuthorizationService.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<Enrollment>(result);
            Assert.AreEqual(status, resp.status);
        }
        #endregion

        #region GetById
        [Test]
        public async Task GetById_HackNotFound()
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = null;
            CancellationToken cancellationToken = default;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.GetById(hack, userId, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test]
        public async Task GetById_EnrollmentNotFound()
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            EnrollmentEntity enrollment = null;
            CancellationToken cancellationToken = default;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", "uid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.GetById(hack, userId, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test]
        public async Task GetById_Succeeded()
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            EnrollmentEntity participant = new EnrollmentEntity
            {
                Status = EnrollmentStatus.pendingApproval,
            };
            CancellationToken cancellationToken = default;
            UserInfo userInfo = new UserInfo();

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", "uid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(userInfo);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
                UserManagement = userManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.GetById(hack, userId, cancellationToken);

            Mock.VerifyAll(hackathonManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();
            Assert.IsTrue(result is OkObjectResult);
            OkObjectResult objectResult = (OkObjectResult)result;
            Enrollment enrollment = (Enrollment)objectResult.Value;
            Assert.IsNotNull(enrollment);
            Assert.AreEqual(EnrollmentStatus.pendingApproval, enrollment.status);
        }
        #endregion

        #region ListEnrollments
        [Test]
        public async Task ListEnrollmentsTest_HackNotFound()
        {
            string hack = "Hack";
            Pagination pagination = new Pagination();
            CancellationToken cancellationToken = CancellationToken.None;
            HackathonEntity hackathonEntity = null;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController
            {
                HackathonManagement = hackathonManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.ListEnrollments(hack, pagination, null, cancellationToken);

            Mock.VerifyAll(hackathonManagement);
            hackathonManagement.VerifyNoOtherCalls();
            AssertHelper.AssertObjectResult(result, 404);
        }

        private static IEnumerable ListEnrollmentsTestData()
        {
            // arg0: pagination
            // arg1: status
            // arg2: mocked TableCotinuationToken
            // arg3: expected options
            // arg4: expected nextlink

            // no pagination, no filter, no top
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    null,
                    new EnrollmentQueryOptions { },
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    EnrollmentStatus.rejected,
                    null,
                    new EnrollmentQueryOptions
                    {
                        Pagination = new Pagination { top = 10, np = "np", nr = "nr" },
                        Status = EnrollmentStatus.rejected
                    },
                    null
                );

            // next link
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    "np nr",
                    new EnrollmentQueryOptions { },
                    "&np=np&nr=nr"
                );
        }

        [Test, TestCaseSource(nameof(ListEnrollmentsTestData))]
        public async Task ListEnrollmentsTest_Succeed(
            Pagination pagination,
            EnrollmentStatus? status,
            string continuationToken,
            EnrollmentQueryOptions expectedOptions,
            string expectedLink)
        {
            // input
            var hackName = "Hack";
            var cancellationToken = CancellationToken.None;
            HackathonEntity hackathonEntity = new HackathonEntity();
            var enrollments = new List<EnrollmentEntity>
            {
                new EnrollmentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    Status = EnrollmentStatus.approved,
                }
            };
            var segment = MockHelper.CreatePage(enrollments, continuationToken);
            UserInfo userInfo = new UserInfo();

            // mock and capture
            var hackathonManagement = new Mock<IHackathonManagement>();
            EnrollmentQueryOptions optionsCaptured = null;
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.ListPaginatedEnrollmentsAsync("hack", It.IsAny<EnrollmentQueryOptions>(), cancellationToken))
                .Callback<string, EnrollmentQueryOptions, CancellationToken>((n, o, t) =>
                {
                    optionsCaptured = o;
                })
                .ReturnsAsync(segment);
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(userInfo);

            // run
            var controller = new EnrollmentController
            {
                ResponseBuilder = new DefaultResponseBuilder(),
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
                UserManagement = userManagement.Object,
            };
            var result = await controller.ListEnrollments(hackName, pagination, status, cancellationToken);

            // verify
            Mock.VerifyAll(hackathonManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();
            var list = AssertHelper.AssertOKResult<EnrollmentList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("rk", list.value[0].userId);
            Assert.AreEqual(EnrollmentStatus.approved, list.value[0].status);
            Assert.AreEqual(expectedOptions.Status, optionsCaptured.Status);
            Assert.AreEqual(expectedOptions.Top(), optionsCaptured.Top());
            Assert.AreEqual(expectedOptions.ContinuationToken(), optionsCaptured.ContinuationToken());
        }
        #endregion
    }
}
