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
                Status = new k8s.Models.V1Status
                {
                    Code = 409,
                    Message = "msg",
                }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(j => j.CreateTemplateAsync(It.Is<Template>(j =>
                j.name == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                AuthorizationService = authorizationService.Object,
                ExperimentManagement = experimentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.CreateTemplate("Hack", parameter, default);

            Mock.VerifyAll(hackathonManagement, authorizationService, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

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
                Status = new k8s.Models.V1Status { Reason = "reason" }
            };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(j => j.CreateTemplateAsync(It.Is<Template>(j =>
                j.name == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                AuthorizationService = authorizationService.Object,
                ExperimentManagement = experimentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.CreateTemplate("Hack", parameter, default);

            Mock.VerifyAll(hackathonManagement, authorizationService, experimentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<Template>(result);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("reason", resp.status.reason);
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

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(j => j.CreateExperimentAsync(It.Is<Experiment>(j =>
                j.templateName == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
                ExperimentManagement = experimentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.CreateExperiment("Hack", parameter, default);

            Mock.VerifyAll(hackathonManagement, experimentManagement, enrollmentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            enrollmentManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

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

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var enrollmentManagement = new Mock<IEnrollmentManagement>();
            enrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            var experimentManagement = new Mock<IExperimentManagement>();
            experimentManagement.Setup(j => j.CreateExperimentAsync(It.Is<Experiment>(j =>
                j.templateName == "default" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);

            var controller = new ExperimentController
            {
                HackathonManagement = hackathonManagement.Object,
                EnrollmentManagement = enrollmentManagement.Object,
                ExperimentManagement = experimentManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.CreateExperiment("Hack", parameter, default);

            Mock.VerifyAll(hackathonManagement, experimentManagement, enrollmentManagement);
            hackathonManagement.VerifyNoOtherCalls();
            enrollmentManagement.VerifyNoOtherCalls();
            experimentManagement.VerifyNoOtherCalls();

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
