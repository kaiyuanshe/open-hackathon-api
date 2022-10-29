using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class QuestionnaireController : HackathonControllerBase
    {
        #region CreateQuestionnaire
        /// <summary>
        /// Add a new Questionnaire.
        /// </summary>
        /// <remarks>
        /// One hackathon provides only one questionnaire.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The created questionnaire.</returns>
        /// <response code="200">Success. The response describes an questionnaire.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Questionnaire), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/questionnaire/")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateQuestionnaire(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, HttpPutPolicy] Questionnaire parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // create
            parameter.hackathonName = hackathonName.ToLower();
            var entity = await QuestionnaireManagement.CreateQuestionnaireAsync(parameter, cancellationToken);

            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.createQuestionnaire, args, cancellationToken);

            var resp = ResponseBuilder.BuildQuestionnaire(entity);
            return Ok(resp);
        }
        #endregion

        #region UpdateQuestionnaire
        /// <summary>
        /// Update an questionnaire.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The updated questionnaire.</returns>
        /// <response code="200">Success. The response describes an questionnaire.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Questionnaire), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/questionnaire/")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> UpdateQuestionnaire(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, Required] Questionnaire parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // query and update.
            var entity = await QuestionnaireManagement.GetQuestionnaireAsync(hackathon.Name, cancellationToken);
            if (entity == null)
            {
                return NotFound(Resources.Questionnaire_NotFound);
            }
            entity = await QuestionnaireManagement.UpdateQuestionnaireAsync(entity, parameter, cancellationToken);

            // logs
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.updateQuestionnaire, args, cancellationToken);

            var resp = ResponseBuilder.BuildQuestionnaire(entity);
            return Ok(resp);
        }
        #endregion

        #region GetQuestionnaire
        /// <summary>
        /// Get an Questionnaire.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The questionnaire.</returns>
        /// <response code="200">Success. The response describes an questionnaire.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Questionnaire), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/questionnaire/")]
        public async Task<object> GetQuestionnaire(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // query
            var entity = await QuestionnaireManagement.GetQuestionnaireAsync(hackathon.Name, cancellationToken);
            if (entity == null)
            {
                return NotFound(Resources.Questionnaire_NotFound);
            }
            var resp = ResponseBuilder.BuildQuestionnaire(entity);
            return Ok(resp);
        }
        #endregion

        #region DeleteQuestionnaire
        /// <summary>
        /// Delete an questionnaire. 
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <response code="204">Deleted</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/questionnaire/")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteQuestionnaire(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }
            Debug.Assert(hackathon != null);

            // query and delete
            var entity = await QuestionnaireManagement.GetQuestionnaireAsync(hackathon.Name, cancellationToken);
            if (entity == null)
            {
                return NoContent();
            }

            await QuestionnaireManagement.DeleteQuestionnaireAsync(entity.HackathonName, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.deleteQuestionnaire, args, cancellationToken);
            return NoContent();
        }
        #endregion
    }
}
