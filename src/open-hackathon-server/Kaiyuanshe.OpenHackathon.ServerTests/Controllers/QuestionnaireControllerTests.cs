using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Moq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    internal class QuestionnaireControllerTests
    {
        #region CreateQuestionnaire
        [Test]
        public async Task CreateQuestionnaireTest_Ok()
        {
            // input
            var hackathonName = "hack";
            var hackathonEntity = new HackathonEntity
            {
                PartitionKey = hackathonName,
            };

            var request = new Questionnaire()
            {
                hackathonName = hackathonName,
            };
            var questionnaireEntity = new QuestionnaireEntity
            {
                PartitionKey = hackathonName,
            };

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(hackathonName, default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.QuestionnaireManagement.Setup(o => o.CreateQuestionnaireAsync(request, default)).ReturnsAsync(questionnaireEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity(hackathonName, It.IsAny<string>(), ActivityLogType.createQuestionnaire, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), hackathonName, It.IsAny<string>(), ActivityLogType.createQuestionnaire, It.IsAny<object>(), null, default));

            // run
            var controller = new QuestionnaireController();
            moqs.SetupController(controller);
            var result = await controller.CreateQuestionnaire(hackathonName, request, default);

            // verify
            moqs.VerifyAll();
            Questionnaire resp = AssertHelper.AssertOKResult<Questionnaire>(result);
            Assert.AreEqual(hackathonName, resp.hackathonName);
        }

        [Test]
        public async Task CreateQuestionnaireTest_HackNotFound()
        {
            var hackathonName = "hack";
            HackathonEntity? hackathonEntity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(hackathonName, default))
                .ReturnsAsync(hackathonEntity);

            var controller = new QuestionnaireController();
            moqs.SetupController(controller);
            var result = await controller.CreateQuestionnaire(hackathonName, new Questionnaire(), default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404);
        }
        #endregion

        #region UpdateQuestionnaire
        [Test]
        public async Task UpdateQuestionnaire_Ok()
        {
            var hackathonName = "hack";
            var hackathonEntity = new HackathonEntity
            {
                PartitionKey = hackathonName,
            };

            var request = new Questionnaire
            {};
            var questionnaireEntity = new QuestionnaireEntity
            {
                PartitionKey = hackathonName,
            };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync(hackathonName, default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.QuestionnaireManagement.Setup(o => o.GetQuestionnaireAsync(hackathonName, default)).ReturnsAsync(questionnaireEntity);
            moqs.QuestionnaireManagement.Setup(o => o.UpdateQuestionnaireAsync(questionnaireEntity, request, default)).ReturnsAsync(questionnaireEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity(hackathonName, "", ActivityLogType.updateQuestionnaire, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", hackathonName, "", ActivityLogType.updateQuestionnaire, It.IsAny<object>(), null, default));

            var controller = new QuestionnaireController();
            moqs.SetupController(controller);
            var result = await controller.UpdateQuestionnaire(hackathonName, request, default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Questionnaire>(result);
            Assert.AreEqual(hackathonName, resp.hackathonName);
        }

        [Test]
        public async Task UpdateQuestionnaire_NotFound()
        {
            var hackathonName = "hack";
            var hackathonEntity = new HackathonEntity
            {
                PartitionKey = hackathonName,
            };

            var request = new Questionnaire
            {};

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.QuestionnaireManagement.Setup(o => o.GetQuestionnaireAsync(hackathonName, default)).ReturnsAsync((QuestionnaireEntity?)null);

            var controller = new QuestionnaireController();
            moqs.SetupController(controller);
            var result = await controller.UpdateQuestionnaire(hackathonName, request, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Questionnaire_NotFound);
        }
        #endregion

        #region GetQuestionnaire
        [TestCase(true)]
        [TestCase(false)]
        public async Task GetQuestionnaire_Ok(bool found)
        {
            var hackathonName = "hack";
            var hackathonEntity = new HackathonEntity
            {
                PartitionKey = hackathonName,
            };

            var questionnaireEntity = found ? new QuestionnaireEntity
            {
                PartitionKey = hackathonName,
            } : null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync(hackathonName, default)).ReturnsAsync(hackathonEntity);
            moqs.QuestionnaireManagement.Setup(o => o.GetQuestionnaireAsync(hackathonName, default)).ReturnsAsync(questionnaireEntity);

            var controller = new QuestionnaireController();
            moqs.SetupController(controller);
            var result = await controller.GetQuestionnaire(hackathonName, default);

            moqs.VerifyAll();
            if (found)
            {
                var resp = AssertHelper.AssertOKResult<Questionnaire>(result);
                Assert.AreEqual(hackathonName, resp.hackathonName);
            }
            else
            {
                AssertHelper.AssertObjectResult(result, 404, Resources.Questionnaire_NotFound);
            }
        }
        #endregion

        #region DeleteQuestionnaire
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteQuestionnaire(bool existed)
        {
            var hackathonName = "hack";
            var hackathonEntity = new HackathonEntity
            {
                PartitionKey = hackathonName,
            };

            var questionnaireEntity = existed ? new QuestionnaireEntity
            {
                PartitionKey = hackathonName,
            } : null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.QuestionnaireManagement.Setup(o => o.GetQuestionnaireAsync(hackathonName, default)).ReturnsAsync(questionnaireEntity);
            if (existed)
            {
                moqs.QuestionnaireManagement.Setup(t => t.DeleteQuestionnaireAsync(hackathonName, default));
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity(hackathonName, It.IsAny<string>(), ActivityLogType.deleteQuestionnaire, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", hackathonName, It.IsAny<string>(), ActivityLogType.deleteQuestionnaire, It.IsAny<object>(), null, default));
            }

            var controller = new QuestionnaireController();
            moqs.SetupController(controller);
            var result = await controller.DeleteQuestionnaire(hackathonName, default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion
    }
}
