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
            HackathonEntity? hackathonEntity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default))
                .ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.Enroll(hackathonName, new Enrollment(), default);

            moqs.VerifyAll();
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
            var result = await controller.Enroll(hackathonName, new Enrollment(), cancellationToken);

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
            var result = await controller.Enroll(hackathonName, new Enrollment(), cancellationToken);

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
            var result = await controller.Enroll(hackathonName, new Enrollment(), cancellationToken);

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
            var result = await controller.Enroll(hackathonName, new Enrollment(), cancellationToken);

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
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), nameof(Resources.ActivityLog_User_updateEnrollment2), default));
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
            Enrollment request = new Enrollment { extensions = new Extension[Questionnaire.MaxExtensions + 1] };
            for (int i = 0; i < Questionnaire.MaxExtensions + 1; i++)
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
            AssertHelper.AssertObjectResult(result, 400, string.Format(Resources.Enrollment_TooManyExtensions, Questionnaire.MaxExtensions));
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
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateEnrollment, It.IsAny<object>(), nameof(Resources.ActivityLog_User_updateEnrollment2), default));
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
            EnrollmentEntity? enrollment = null;

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

            Enrollment en = AssertHelper.AssertOKResult<Enrollment>(result);
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

            throw new ArgumentOutOfRangeException();
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_HackNotFound(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity? hackathonEntity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, new Enrollment(), default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_Forbidden(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Failed();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, new Enrollment(), default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403);
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_EnrollmentNotFound(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            EnrollmentEntity? participant = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", "uid", default)).ReturnsAsync(participant);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, new Enrollment(), default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404);
        }

        [Test, TestCaseSource(nameof(ListStatus))]
        public async Task ApproveRejectTest_Succeeded(EnrollmentStatus status)
        {
            string hack = "Hack";
            string userId = "Uid";
            HackathonEntity hackathonEntity = new HackathonEntity { PartitionKey = "test2" };
            var authResult = AuthorizationResult.Success();
            EnrollmentEntity enrollment = new EnrollmentEntity
            {
                PartitionKey = "pk",
                Status = EnrollmentStatus.pendingApproval,
            };
            UserInfo userInfo = new UserInfo();

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "uid", default)).ReturnsAsync(enrollment);
            moqs.EnrollmentManagement.Setup(p => p.UpdateEnrollmentStatusAsync(hackathonEntity, It.IsAny<EnrollmentEntity>(), It.IsAny<EnrollmentStatus>(), default))
                .Callback<HackathonEntity, EnrollmentEntity, EnrollmentStatus, CancellationToken>((h, p, e, c) =>
                {
                    p.Status = e;
                })
                .ReturnsAsync(enrollment);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default)).ReturnsAsync(userInfo);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            var activityLogType = status == EnrollmentStatus.approved ? ActivityLogType.approveEnrollment : ActivityLogType.rejectEnrollment;
            var resKeyOfUser = status == EnrollmentStatus.approved ? nameof(Resources.ActivityLog_User_approveEnrollment2) : nameof(Resources.ActivityLog_User_rejectEnrollment2);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", activityLogType, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", activityLogType, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("uid", "test2", "", activityLogType, It.IsAny<object>(), resKeyOfUser, default));

            // test
            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var func = GetTargetMethod(controller, status);
            var result = await func(hack, userId, new Enrollment(), default);

            // verify
            moqs.VerifyAll();
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
            HackathonEntity? hackathonEntity = null;
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
            EnrollmentEntity? enrollment = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", "uid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.GetById(hack, userId, default);

            moqs.VerifyAll();
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
            UserInfo userInfo = new UserInfo();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", "uid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(participant);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), default))
                .ReturnsAsync(userInfo);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.GetById(hack, userId, default);

            moqs.VerifyAll();
            Enrollment enrollment = AssertHelper.AssertOKResult<Enrollment>(result);
            Assert.AreEqual(EnrollmentStatus.pendingApproval, enrollment.status);
        }
        #endregion

        #region ListEnrollments
        [Test]
        public async Task ListEnrollmentsTest_HackNotFound()
        {
            string hack = "Hack";
            Pagination pagination = new Pagination();
            HackathonEntity? hackathonEntity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);

            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.ListEnrollments(hack, pagination, null, default);

            moqs.VerifyAll();
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
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            moqs.EnrollmentManagement.Setup(p => p.ListPaginatedEnrollmentsAsync("hack", It.Is<EnrollmentQueryOptions>(o =>
                o.Status == expectedOptions.Status
                && o.ContinuationToken() == expectedOptions.ContinuationToken()
                && o.Top(100) == expectedOptions.Top(100)), cancellationToken))
                .ReturnsAsync(segment);
            moqs.UserManagement.Setup(p => p.GetUserByIdAsync(It.IsAny<string>(), cancellationToken))
                .ReturnsAsync(userInfo);

            // run
            var controller = new EnrollmentController();
            moqs.SetupController(controller);
            var result = await controller.ListEnrollments(hackName, pagination, status, cancellationToken);

            // verify
            moqs.VerifyAll();
            var list = AssertHelper.AssertOKResult<EnrollmentList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("rk", list.value[0].userId);
            Assert.AreEqual(EnrollmentStatus.approved, list.value[0].status);
        }
        #endregion
    }
}
