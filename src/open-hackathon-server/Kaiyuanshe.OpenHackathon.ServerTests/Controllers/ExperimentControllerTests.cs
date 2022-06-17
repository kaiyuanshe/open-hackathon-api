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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    class ExperimentControllerTests
    {
        #region CreateTemplate
        [Test]
        public async Task CreateTemplate_TooMany()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateCountAsync("hack", default)).ReturnsAsync(ExperimentController.MaxTemplatePerHackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateTemplate("Hack", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, string.Format(Resources.Template_ExceedMax, ExperimentController.MaxTemplatePerHackathon));
        }

        [Test]
        public async Task CreateTemplate_K8SFailure()
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
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
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateCountAsync("hack", default)).ReturnsAsync(0);
            moqs.ExperimentManagement.Setup(j => j.CreateOrUpdateTemplateAsync(It.Is<Template>(j =>
                j.id == null &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.createTemplate, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.createTemplate, It.IsAny<object>(), null, default));

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateTemplate("Hack", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 409, "msg");
        }

        [Test]
        public async Task CreateTemplate_Success()
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };
            var entity = new TemplateEntity { PartitionKey = "pk" };
            var context = new TemplateContext
            {
                TemplateEntity = entity,
                Status = new V1Status { Reason = "reason" }
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateCountAsync("hack", default)).ReturnsAsync(0);
            moqs.ExperimentManagement.Setup(j => j.CreateOrUpdateTemplateAsync(It.Is<Template>(j =>
                j.id == null &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.createTemplate, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.createTemplate, It.IsAny<object>(), null, default));

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateTemplate("Hack", parameter, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Template>(result);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("reason", resp.status.reason);
        }
        #endregion

        #region UpdateTemplate
        [Test]
        public async Task UpdateTemplate_NotFound()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };
            var entity = new TemplateEntity { PartitionKey = "pk" };
            TemplateContext context = null;

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "any", default)).ReturnsAsync(context);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.UpdateTemplate("Hack", "any", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Template_NotFound, "any", "Hack"));
        }

        [Test]
        public async Task UpdateTemplate_NotFound2()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };
            var entity = new TemplateEntity { PartitionKey = "pk" };
            var context = new TemplateContext
            {
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "any", default)).ReturnsAsync(context);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.UpdateTemplate("Hack", "any", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Template_NotFound, "any", "Hack"));
        }

        [Test]
        public async Task UpdateTemplate_K8SFailure()
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
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
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "any", default)).ReturnsAsync(context);
            moqs.ExperimentManagement.Setup(j => j.CreateOrUpdateTemplateAsync(It.Is<Template>(j =>
                j.id == "any" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.updateTemplate, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.updateTemplate, It.IsAny<object>(), null, default));
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.UpdateTemplate("Hack", "any", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 409, "msg");
        }

        [Test]
        public async Task UpdateTemplate_Success()
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Template { };
            var entity = new TemplateEntity { PartitionKey = "pk" };
            var context = new TemplateContext
            {
                TemplateEntity = entity,
                Status = new V1Status { Reason = "reason" }
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "any", default)).ReturnsAsync(context);
            moqs.ExperimentManagement.Setup(j => j.CreateOrUpdateTemplateAsync(It.Is<Template>(j =>
                j.id == "any" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.updateTemplate, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.updateTemplate, It.IsAny<object>(), null, default));
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.UpdateTemplate("Hack", "any", parameter, default);

            // verify
            moqs.VerifyAll();
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
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "tpl", default)).ReturnsAsync(context);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.GetTemplate("Hack", "tpl", default);

            // verify
            Mock.VerifyAll(moqs.HackathonManagement,
                moqs.ExperimentManagement,
                moqs.AuthorizationService);
            moqs.HackathonManagement.VerifyNoOtherCalls();
            moqs.ExperimentManagement.VerifyNoOtherCalls();
            moqs.AuthorizationService.VerifyNoOtherCalls();

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
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "tpl", default)).ReturnsAsync(context);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.GetTemplate("Hack", "tpl", default);

            // verify
            Mock.VerifyAll(moqs.HackathonManagement,
                moqs.ExperimentManagement,
                moqs.AuthorizationService);
            moqs.HackathonManagement.VerifyNoOtherCalls();
            moqs.ExperimentManagement.VerifyNoOtherCalls();
            moqs.AuthorizationService.VerifyNoOtherCalls();

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
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.GetTemplateAsync("hack", "tpl", default)).ReturnsAsync(context);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.GetTemplate("Hack", "tpl", default);

            // verify
            Mock.VerifyAll(moqs.HackathonManagement,
                moqs.ExperimentManagement,
                moqs.AuthorizationService);
            moqs.HackathonManagement.VerifyNoOtherCalls();
            moqs.ExperimentManagement.VerifyNoOtherCalls();
            moqs.AuthorizationService.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<Template>(result);
            Assert.AreEqual("pk", resp.hackathonName);
        }
        #endregion

        #region ListTemplates
        [Test]
        public async Task ListTemplates()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            List<TemplateContext> contexts = new List<TemplateContext>
            {
                new TemplateContext
                {
                    TemplateEntity = new TemplateEntity
                    {
                        DisplayName = "dn"
                    },
                    Status = new V1Status
                    {
                        Code = 200
                    }
                }
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.ListTemplatesAsync("hack", default)).ReturnsAsync(contexts);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.ListTemplates("Hack", default);

            // verify
            moqs.VerifyAll();
            var obj = AssertHelper.AssertOKResult<TemplateList>(result);
            Assert.AreEqual(1, obj.value.Count());
            Assert.AreEqual("dn", obj.value.First().displayName);
            Assert.AreEqual(200, obj.value.First().status.code);
            Assert.IsNull(obj.nextLink);
        }
        #endregion

        #region DeleteTemplate
        [Test]
        public async Task DeleteTemplate_HasExpr()
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var authResult = AuthorizationResult.Success();
            var experiments = new List<ExperimentContext>
            {
                new ExperimentContext{ }
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.ListExperimentsAsync(hackathon, "tpl", default)).ReturnsAsync(experiments);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.DeleteTemplate("Hack", "tpl", default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Template_HasExperiment);
        }

        private static IEnumerable DeleteTemplateTestData()
        {
            // arg0: TemplateContext
            // arg1: expected http code
            // arg2: expected http message

            // null context
            yield return new TestCaseData(
                    null,
                    204,
                    null
                );

            // no entity/status
            yield return new TestCaseData(
                    new TemplateContext { },
                    204,
                    null
                );

            // status < 400
            yield return new TestCaseData(
                    new TemplateContext { Status = new V1Status { Code = 200 } },
                    204,
                    null
                );

            // status >= 400
            yield return new TestCaseData(
                    new TemplateContext { Status = new V1Status { Code = 400, Message = "error" } },
                    400,
                    "error"
                );
            yield return new TestCaseData(
                    new TemplateContext { Status = new V1Status { Code = 500, Message = "error" } },
                    500,
                    "error"
                );

            // with entity -> trigger activity logs
            yield return new TestCaseData(
                    new TemplateContext { TemplateEntity = new(), Status = new V1Status { Code = 500, Message = "error" } },
                    500,
                    "error"
                );
        }

        [Test, TestCaseSource(nameof(DeleteTemplateTestData))]
        public async Task DeleteTemplate(TemplateContext templateContext, int expectedCode, string expectedMessage)
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var authResult = AuthorizationResult.Success();

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.DeleteTemplateAsync("hack", "tpl", default)).ReturnsAsync(templateContext);
            moqs.ExperimentManagement.Setup(j => j.ListExperimentsAsync(hackathon, "tpl", default)).ReturnsAsync(new ExperimentContext[0]);
            if (templateContext?.TemplateEntity != null)
            {
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.deleteTemplate, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.deleteTemplate, It.IsAny<object>(), null, default));
            }

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.DeleteTemplate("Hack", "tpl", default);

            // verify
            moqs.VerifyAll();
            if (expectedCode == 204)
            {
                AssertHelper.AssertNoContentResult(result);
            }
            else
            {
                AssertHelper.AssertObjectResult(result, expectedCode, expectedMessage);
            }
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

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);

            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            moqs.VerifyAll();
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

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.EnrollmentManagement.Setup(p => p.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);

            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Enrollment_NotApproved);
        }

        [Test]
        public async Task CreateExperiment_TemplateInvalid()
        {
            var parameter = new Experiment { templateId = "tplId" };
            var hackathon = new HackathonEntity();
            var experiment = new ExperimentEntity { RowKey = "rk" };
            EnrollmentEntity enrollment = new EnrollmentEntity { Status = EnrollmentStatus.approved };
            UserInfo userInfo = new UserInfo { Region = "region" };
            var templateContext = new TemplateContext { Status = new V1Status { Code = 422, Message = "bad" } };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            moqs.ExperimentManagement.Setup(e => e.GetTemplateAsync("hack", "tplId", default)).ReturnsAsync(templateContext);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 422, "bad");
        }

        [Test]
        public async Task CreateExperiment_K8SFailure()
        {
            var parameter = new Experiment { templateId = "tplId" };
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
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
            var templateContext = new TemplateContext { TemplateEntity = new(), Status = new V1Status { Code = 200 } };


            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            moqs.ExperimentManagement.Setup(j => j.CreateOrUpdateExperimentAsync(It.Is<Experiment>(j =>
                j.templateId == "tplId" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            moqs.ExperimentManagement.Setup(e => e.GetTemplateAsync("hack", "tplId", default)).ReturnsAsync(templateContext);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.createExperiment, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.createExperiment, It.IsAny<object>(), null, default));

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 500, "msg");
        }

        [Test]
        public async Task CreateExperiment_Success()
        {
            var parameter = new Experiment { templateId = "tplId" };
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var experiment = new ExperimentEntity { RowKey = "rk" };
            EnrollmentEntity enrollment = new EnrollmentEntity { Status = EnrollmentStatus.approved };
            var context = new ExperimentContext
            {
                ExperimentEntity = experiment,
                Status = new ExperimentStatus { Reason = "reason" }
            };
            UserInfo userInfo = new UserInfo { Region = "region" };
            var templateContext = new TemplateContext { TemplateEntity = new(), Status = new V1Status { Code = 200 } };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", It.IsAny<string>(), default)).ReturnsAsync(enrollment);
            moqs.ExperimentManagement.Setup(j => j.CreateOrUpdateExperimentAsync(It.Is<Experiment>(j =>
                j.templateId == "tplId" &&
                j.hackathonName == "hack"), default)).ReturnsAsync(context);
            moqs.ExperimentManagement.Setup(e => e.GetTemplateAsync("hack", "tplId", default)).ReturnsAsync(templateContext);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.createExperiment, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.createExperiment, It.IsAny<object>(), null, default));

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.CreateExperiment("Hack", parameter, default);

            // verify
            moqs.VerifyAll();
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
                ExperimentEntity = new ExperimentEntity { UserId = "", TemplateId = "tn" },
                Status = new ExperimentStatus
                {
                    protocol = IngressProtocol.vnc,
                    ingressPort = 5902
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
                ExperimentEntity = new ExperimentEntity { UserId = "", TemplateId = "tn" },
                Status = new ExperimentStatus
                {
                    protocol = IngressProtocol.vnc,
                    ingressPort = 5902
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

        #region GetExperiment
        [Test]
        public async Task GetExperiment_ExpNotFound()
        {
            var hackathon = new HackathonEntity { };
            var expContext = new ExperimentContext { };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "exp", default)).ReturnsAsync(expContext);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.GetExperiment("Hack", "exp", default);

            // verify
            Mock.VerifyAll(moqs.HackathonManagement,
                moqs.ExperimentManagement);
            moqs.HackathonManagement.VerifyNoOtherCalls();
            moqs.ExperimentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, Resources.Experiment_NotFound);
        }

        [Test]
        public async Task GetExperiment_ExpFailed()
        {
            var hackathon = new HackathonEntity { };
            var expContext = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity { },
                Status = new ExperimentStatus
                {
                    Code = 422,
                    Message = "msg"
                },
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "exp", default)).ReturnsAsync(expContext);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.GetExperiment("Hack", "exp", default);

            // verify
            Mock.VerifyAll(moqs.HackathonManagement,
                moqs.ExperimentManagement);
            moqs.HackathonManagement.VerifyNoOtherCalls();
            moqs.ExperimentManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 422, "msg");
        }

        [Test]
        public async Task GetExperiment_Succeeded()
        {
            var hackathon = new HackathonEntity { };
            var expContext = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    UserId = "uid",
                },
                Status = new ExperimentStatus
                {
                    Code = 200,
                },
            };
            var user = new UserInfo { Name = "un" };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "exp", default)).ReturnsAsync(expContext);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.GetExperiment("Hack", "exp", default);

            // verify
            Mock.VerifyAll(moqs.HackathonManagement,
                moqs.ExperimentManagement,
                moqs.UserManagement);
            moqs.HackathonManagement.VerifyNoOtherCalls();
            moqs.ExperimentManagement.VerifyNoOtherCalls();
            moqs.UserManagement.VerifyNoOtherCalls();

            var exp = AssertHelper.AssertOKResult<Experiment>(result);
            Assert.AreEqual("un", exp.user.Name);
            Assert.AreEqual("uid", exp.userId);
        }
        #endregion

        #region ResetExperiment
        [Test]
        public async Task ResetExperiment_ExpNotFound()
        {
            var expContext = new ExperimentContext { };

            // mock
            var moqs = new Moqs();
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "expr", default)).ReturnsAsync(expContext);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.ResetExperiment("Hack", "expr", default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Experiment_NotFound);
        }

        [Test]
        public async Task ResetExperiment_HackNotFound()
        {
            var expContext = new ExperimentContext { ExperimentEntity = new(), };
            HackathonEntity hackathon = null;

            // mock
            var moqs = new Moqs();
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "expr", default)).ReturnsAsync(expContext);
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.ResetExperiment("Hack", "expr", default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, "Hack"));
        }

        [Test]
        public async Task ResetExperiment_AccessDenied()
        {
            var expContext = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    UserId = "uid"
                },
            };
            HackathonEntity hackathon = new();
            var authResult = AuthorizationResult.Failed();

            // mock
            var moqs = new Moqs();
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "expr", default)).ReturnsAsync(expContext);
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.ResetExperiment("Hack", "expr", default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [TestCase(200)]
        [TestCase(409)]
        public async Task ResetExperiment_ResetByAdmin(int statusFromK8s)
        {
            var expContext = new ExperimentContext
            {
                ExperimentEntity = new ExperimentEntity
                {
                    UserId = "uid",
                    TemplateId = "tpl",
                },
                Status = new ExperimentStatus
                {
                    Code = statusFromK8s,
                    Message = "error",
                }
            };
            HackathonEntity hackathon = new() { PartitionKey = "hack" };
            var authResult = AuthorizationResult.Success();
            var user = new UserInfo { Name = "un" };

            // mock
            var moqs = new Moqs();
            moqs.ExperimentManagement.Setup(e => e.GetExperimentAsync("hack", "expr", default)).ReturnsAsync(expContext);
            moqs.ExperimentManagement.Setup(e => e.ResetExperimentAsync("hack", "expr", default)).ReturnsAsync(expContext);
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            if (statusFromK8s < 400)
            {
                moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            }
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.resetExperiment, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.resetExperiment, It.IsAny<object>(), null, default));

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.ResetExperiment("Hack", "expr", default);

            // verify
            moqs.VerifyAll();
            if (statusFromK8s >= 400)
            {
                AssertHelper.AssertObjectResult(result, statusFromK8s, "error");
            }
            else
            {
                Experiment resp = AssertHelper.AssertOKResult<Experiment>(result);
                Assert.AreEqual("tpl", resp.templateId);
                Assert.AreEqual("un", resp.user.Name);
            }
        }
        #endregion

        #region ListExperiments
        [Test]
        public async Task ListExperiments()
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            List<ExperimentContext> contexts = new List<ExperimentContext>
            {
                new ExperimentContext
                {
                    ExperimentEntity = new ExperimentEntity
                    {
                        UserId="uid",
                    },
                    Status = new ExperimentStatus
                    {
                        Code = 200
                    }
                }
            };
            var userInfo = new UserInfo { Name = "un" };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.ExperimentManagement.Setup(j => j.ListExperimentsAsync(hackathon, "tpl", default)).ReturnsAsync(contexts);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(userInfo);

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.ListExperiments("Hack", "tpl", default);

            // verify
            moqs.VerifyAll();

            var obj = AssertHelper.AssertOKResult<ExperimentList>(result);
            Assert.AreEqual(1, obj.value.Count());
            Assert.AreEqual("un", obj.value.First().user.Name);
            Assert.AreEqual(200, obj.value.First().status.code);
            Assert.IsNull(obj.nextLink);
        }
        #endregion

        #region DeleteExperiment

        private static IEnumerable DeleteExperimentTestData()
        {
            // arg0: ExperimentContext
            // arg1: expected http code
            // arg2: expected http message

            // null context
            yield return new TestCaseData(
                    null,
                    204,
                    null
                );

            // no status
            yield return new TestCaseData(
                    new ExperimentContext { },
                    204,
                    null
                );

            // status < 400
            yield return new TestCaseData(
                    new ExperimentContext { Status = new ExperimentStatus { Code = 200 } },
                    204,
                    null
                );

            // status < 400 and entity found
            yield return new TestCaseData(
                    new ExperimentContext
                    {
                        Status = new ExperimentStatus { Code = 200 },
                        ExperimentEntity = new ExperimentEntity(),
                    },
                    204,
                    null
                );

            // status >= 400
            yield return new TestCaseData(
                    new ExperimentContext { Status = new ExperimentStatus { Code = 400, Message = "error" } },
                    400,
                    "error"
                );
            yield return new TestCaseData(
                    new ExperimentContext { Status = new ExperimentStatus { Code = 500, Message = "error" } },
                    500,
                    "error"
                );
        }

        [Test, TestCaseSource(nameof(DeleteExperimentTestData))]
        public async Task DeleteExperiment(ExperimentContext context, int expectedCode, string expectedMessage)
        {
            var hackathon = new HackathonEntity { PartitionKey = "hack" };
            var authResult = AuthorizationResult.Success();

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.ExperimentManagement.Setup(j => j.DeleteExperimentAsync("hack", "expr", default)).ReturnsAsync(context);
            if (context?.ExperimentEntity != null)
            {
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.deleteExperiment, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.deleteExperiment, It.IsAny<object>(), null, default));
            }

            // test
            var controller = new ExperimentController();
            moqs.SetupController(controller);
            var result = await controller.DeleteExperiment("Hack", "expr", default);

            // verify
            moqs.VerifyAll();
            if (expectedCode == 204)
            {
                AssertHelper.AssertNoContentResult(result);
            }
            else
            {
                AssertHelper.AssertObjectResult(result, expectedCode, expectedMessage);
            }
        }
        #endregion
    }
}
