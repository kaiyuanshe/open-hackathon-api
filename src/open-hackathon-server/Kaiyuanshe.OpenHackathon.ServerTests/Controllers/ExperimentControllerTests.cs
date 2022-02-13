using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    class ExperimentControllerTests
    {
        #region CreateTemplate
        [Test]
        public async Task CreateTemplate_K8SFailure()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };
            var entity = new TemplateEntity { PartitionKey = "pk" };
            var context = new TemplateContext
            {
                TemplateEntity = entity,
                Status = new V1Status
                {
                    Code = 409,
                    Message = "msg",
                }
            };

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.ExperimentManagement.Setup(j => j.CreateOrUpdateTemplateAsync(It.Is<Template>(j =>
                j.name == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            mockContext.ActivityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                && a.ActivityLogType == ActivityLogType.createTemplate.ToString()
                && a.Message == "msg"), default));
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.CreateTemplate("Hack", parameter, default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.AuthorizationService,
                mockContext.ActivityLogManagement);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.AuthorizationService.VerifyNoOtherCalls();
            mockContext.ActivityLogManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 409, "msg");
        }

        [Test]
        public async Task CreateTemplate_Success()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };
            var entity = new TemplateEntity { PartitionKey = "pk" };
            var context = new TemplateContext
            {
                TemplateEntity = entity,
                Status = new V1Status { Reason = "reason" }
            };

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.ExperimentManagement.Setup(j => j.CreateOrUpdateTemplateAsync(It.Is<Template>(j =>
                j.name == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            mockContext.ActivityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                && a.ActivityLogType == ActivityLogType.createTemplate.ToString()
                && a.Message == null), default));
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.CreateTemplate("Hack", parameter, default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.AuthorizationService,
                mockContext.ActivityLogManagement);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.AuthorizationService.VerifyNoOtherCalls();
            mockContext.ActivityLogManagement.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<Template>(result);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("reason", resp.status.reason);
        }
        #endregion

        #region GetTemplate
        [Test]
        public async Task GetTemplate_NotFound()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            TemplateContext context = null;

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            mockContext.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "tpl", default)).ReturnsAsync(context);

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.GetTemplate("Hack", "tpl", default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.AuthorizationService);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.AuthorizationService.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Template_NotFound, "tpl", "Hack"));
        }

        [Test]
        public async Task GetTemplate_K8SFailure()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            TemplateContext context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { },
                Status = new V1Status
                {
                    Code = 429,
                    Message = "msg"
                }
            };

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            mockContext.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "tpl", default)).ReturnsAsync(context);

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.GetTemplate("Hack", "tpl", default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.AuthorizationService);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.AuthorizationService.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 429, "msg");
        }

        [Test]
        public async Task GetTemplate_Success()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            TemplateContext context = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { PartitionKey = "pk" },
            };

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            mockContext.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "tpl", default)).ReturnsAsync(context);

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.GetTemplate("Hack", "tpl", default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.AuthorizationService);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.AuthorizationService.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<Template>(result);
            Assert.AreEqual("pk", resp.hackathonName);
        }
        #endregion

        #region CreateExperiment
        [Test]
        public async Task CreateExperiment_NotEnrolled()
        {
            var parameter = new Experiment();
            var hackathon = new HackathonEntity();
            var experiment = new ExperimentEntity { };
            EnrollmentEntity enrollment = null;
            var context = new ExperimentContext
            {
                ExperimentEntity = experiment,
                Status = new ExperimentStatus { Reason = "reason" }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
            };
            var result = await controller.CreateExperiment("Hack", parameter, default);

            Mock.VerifyAll(hackathonManagement, enrollmentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            enrollmentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Enrollment_NotFound, "", "Hack"));
        }

        [Test]
        public async Task CreateExperiment_EnrollentNotApproved()
        {
            var parameter = new Experiment();
            var hackathon = new HackathonEntity();
            var experiment = new ExperimentEntity { };
            EnrollmentEntity enrollment = new EnrollmentEntity { Status = EnrollmentStatus.pendingApproval };
            var context = new ExperimentContext
            {
                ExperimentEntity = experiment,
                Status = new ExperimentStatus { Reason = "reason" }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
            };
            var result = await controller.CreateExperiment("Hack", parameter, default);

            Mock.VerifyAll(hackathonManagement, enrollmentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            enrollmentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 412, Resources.Enrollment_NotApproved);
        }

        [Test]
        public async Task CreateExperiment_K8SFailure()
        {
            var parameter = new Experiment();
            var hackathon = new HackathonEntity();
            var experiment = new ExperimentEntity { RowKey = "rk" };
            EnrollmentEntity enrollment = new EnrollmentEntity { Status = EnrollmentStatus.approved };
            var context = new ExperimentContext
            {
                ExperimentEntity = experiment,
                Status = new ExperimentStatus
                {
                    Code = 500,
                    Message = "msg"
                }
            };
            UserInfo userInfo = new UserInfo { Region = "region" };


            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            mockContext.ExperimentManagement.Setup(j => j.CreateExperimentAsync(It.Is<Experiment>(j =>
                j.templateName == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            mockContext.ActivityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                && a.ActivityLogType == ActivityLogType.createExperiment.ToString()
                && a.Message == "msg"), default));

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.EnrollmentManagement,
                mockContext.ActivityLogManagement);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.EnrollmentManagement.VerifyNoOtherCalls();
            mockContext.ActivityLogManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 500, "msg");
        }

        [Test]
        public async Task CreateExperiment_Success()
        {
            var parameter = new Experiment();
            var hackathon = new HackathonEntity();
            var experiment = new ExperimentEntity { RowKey = "rk" };
            EnrollmentEntity enrollment = new EnrollmentEntity { Status = EnrollmentStatus.approved };
            var context = new ExperimentContext
            {
                ExperimentEntity = experiment,
                Status = new ExperimentStatus { Reason = "reason" }
            };
            UserInfo userInfo = new UserInfo { Region = "region" };

            // mock
            var mockContext = new MockControllerContext();
            mockContext.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            mockContext.ExperimentManagement.Setup(j => j.CreateExperimentAsync(It.Is<Experiment>(j =>
                j.templateName == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            mockContext.ActivityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                && a.ActivityLogType == ActivityLogType.createExperiment.ToString()
                && a.Message == null), default));

            // test
            var controller = new ExperimentController();
            mockContext.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            // verify
            Mock.VerifyAll(mockContext.HackathonManagement,
                mockContext.ExperimentManagement,
                mockContext.EnrollmentManagement,
                mockContext.ActivityLogManagement);
            mockContext.HackathonManagement.VerifyNoOtherCalls();
            mockContext.ExperimentManagement.VerifyNoOtherCalls();
            mockContext.EnrollmentManagement.VerifyNoOtherCalls();
            mockContext.ActivityLogManagement.VerifyNoOtherCalls();

            Experiment resp = AssertHelper.AssertOKResult<Experiment>(result);
            Assert.AreEqual("rk", resp.id);
            Assert.AreEqual("reason", resp.status.reason);
        }
        #endregion

        #region GetConnections
        [Test]
        public async Task GetConnections_ExpNotFound()
        {
            var hackathon = new HackathonEntity { };
            var experiment = new ExperimentContext { };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(e => e.GetExperimentAsync("hack", "eid", default)).ReturnsAsync(experiment);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                ExperimentManagement = experimentManagement.Object,
            };
            var result = await controller.GetConnections("hack", "eid", default);

            Mock.VerifyAll(hackathonManagement, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, Resources.Experiment_NotFound);
        }

        [Test]
        public async Task GetConnections_UserNotMatch()
        {
            var hackathon = new HackathonEntity { };
            var experiment = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    UserId = "other"
                }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(e => e.GetExperimentAsync("hack", "eid", default)).ReturnsAsync(experiment);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                ExperimentManagement = experimentManagement.Object,
            };
            var result = await controller.GetConnections("hack", "eid", default);

            Mock.VerifyAll(hackathonManagement, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 403, Resources.Experiment_UserNotMatch);
        }

        [Test]
        public async Task GetConnections_ExpFailed()
        {
            var hackathon = new HackathonEntity { };
            var experiment = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { UserId = "" },
                Status = new ExperimentStatus { Code = 400, Message = "msg" }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(e => e.GetExperimentAsync("hack", "eid", default)).ReturnsAsync(experiment);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                ExperimentManagement = experimentManagement.Object,
            };
            var result = await controller.GetConnections("hack", "eid", default);

            Mock.VerifyAll(hackathonManagement, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 400, "msg");
        }

        [Test]
        public async Task GetConnections_SucceedWithoutTemplate()
        {
            var hackathon = new HackathonEntity { };
            var experiment = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { UserId = "", TemplateName = "tn" },
                Status = new ExperimentStatus
                {
                    IngressProtocol = IngressProtocol.vnc,
                    IngressPort = 5902
                }
            };
            var template = new TemplateContext
            {
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(e => e.GetExperimentAsync("hack", "eid", default)).ReturnsAsync(experiment);
            experimentManagement.Setup(e => e.GetTemplateAsync("hack", "tn", default)).ReturnsAsync(template);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                ExperimentManagement = experimentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.GetConnections("hack", "eid", default);

            Mock.VerifyAll(hackathonManagement, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

            var list = AssertHelper.AssertOKResult<GuacamoleConnectionList>(result);
            Assert.AreEqual(1, list.value.Length);
            VncConnection vncConnection = list.value[0] as VncConnection;
            Assert.IsNotNull(vncConnection);
            Assert.AreEqual(IngressProtocol.vnc, vncConnection.protocol);
            Assert.AreEqual(5902, vncConnection.port);
            Assert.AreEqual("tn", vncConnection.name);
        }

        [Test]
        public async Task GetConnections_SucceedWithTemplate()
        {
            var hackathon = new HackathonEntity { };
            var experiment = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { UserId = "", TemplateName = "tn" },
                Status = new ExperimentStatus
                {
                    IngressProtocol = IngressProtocol.vnc,
                    IngressPort = 5902
                }
            };
            var template = new TemplateContext
            {
                TemplateEntity = new TemplateEntity { DisplayName = "display" }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(e => e.GetExperimentAsync("hack", "eid", default)).ReturnsAsync(experiment);
            experimentManagement.Setup(e => e.GetTemplateAsync("hack", "tn", default)).ReturnsAsync(template);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                ExperimentManagement = experimentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.GetConnections("hack", "eid", default);

            Mock.VerifyAll(hackathonManagement, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

            var list = AssertHelper.AssertOKResult<GuacamoleConnectionList>(result);
            Assert.AreEqual(1, list.value.Length);
            VncConnection vncConnection = list.value[0] as VncConnection;
            Assert.IsNotNull(vncConnection);
            Assert.AreEqual(IngressProtocol.vnc, vncConnection.protocol);
            Assert.AreEqual(5902, vncConnection.port);
            Assert.AreEqual("display", vncConnection.name);
        }
        #endregion
    }
}
