using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using Kaiyuanshe.OpenHackathon.Server.K8S.Models;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class ExperimentController : HackathonControllerBase
    {
        public static readonly int MaxTemplatePerHackathon = 100;

        #region CreateTemplate
        /// <summary>
        /// Create a hackathon template which can be used to setup a vitual experiment on cloud.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The award</returns>
        /// <response code="200">Success. The response describes a template.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Template), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/template")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateTemplate(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Template parameter,
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

            // validate count
            var templateCount = await ExperimentManagement.GetTemplateCountAsync(hackathonName.ToLower(), cancellationToken);
            if (templateCount >= MaxTemplatePerHackathon)
            {
                return PreconditionFailed(string.Format(Resources.Template_ExceedMax, MaxTemplatePerHackathon));
            }

            // create template
            parameter.hackathonName = hackathonName.ToLower();
            var context = await ExperimentManagement.CreateOrUpdateTemplateAsync(parameter, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.createTemplate.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = context?.Status?.Message,
            }, cancellationToken);
            var user = await UserManagement.GetUserByIdAsync(CurrentUserId, cancellationToken);
            if (context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: context.TemplateEntity.DisplayName);
            }
            else
            {
                return Ok(ResponseBuilder.BuildTemplate(context));
            }
        }
        #endregion

        #region UpdateTemplate
        /// <summary>
        /// Update a hackathon template.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="templateId" example="1009bb6-be04-4ba1-901b-21e2b5b0f714">Auto-generated Id of the template. Clients can get the Id in a Create, Update or List request.</param>
        /// <returns>The updated template.</returns>
        /// <response code="200">Success. The response describes a template.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Template), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/template/{templateId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> UpdateTemplate(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string templateId,
            [FromBody] Template parameter,
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

            // validate template exist
            var template = await ExperimentManagement.GetTemplateAsync(hackathonName.ToLower(), templateId, cancellationToken);
            if (template?.TemplateEntity == null)
            {
                return NotFound(string.Format(Resources.Template_NotFound, templateId, hackathonName));
            }

            // update template
            parameter.hackathonName = hackathonName.ToLower();
            parameter.id = templateId;
            var context = await ExperimentManagement.CreateOrUpdateTemplateAsync(parameter, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.updateTemplate.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = context?.Status?.Message,
            }, cancellationToken);
            var user = await UserManagement.GetUserByIdAsync(CurrentUserId, cancellationToken);
            if (context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: context.TemplateEntity.DisplayName);
            }
            else
            {
                return Ok(ResponseBuilder.BuildTemplate(context));
            }
        }
        #endregion

        #region GetTemplate
        /// <summary>
        /// Query a hackathon template by id.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="templateId" example="1009bb6-be04-4ba1-901b-21e2b5b0f714">Auto-generated Id of the template. Clients can get the Id in a Create, Update or List request.</param>
        /// <returns>The template.</returns>
        /// <response code="200">Success. The response describes a template.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Template), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/template/{templateId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> GetTemplate(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string templateId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // get template
            var context = await ExperimentManagement.GetTemplateAsync(hackathonName.ToLower(), templateId, cancellationToken);
            if (context == null)
            {
                return NotFound(string.Format(Resources.Template_NotFound, templateId, hackathonName));
            }
            if (context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: context.TemplateEntity.DisplayName);
            }
            else
            {
                return Ok(ResponseBuilder.BuildTemplate(context));
            }
        }
        #endregion

        #region ListTemplates
        /// <summary>
        /// List templates of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The templates.</returns>
        /// <response code="200">Success. The response describes a list of templates. All templates returned in one request, no pagination support.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TemplateList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/templates")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> ListTemplates(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // list templates
            var contexts = await ExperimentManagement.ListTemplatesAsync(hackathonName.ToLower(), cancellationToken);
            var resp = ResponseBuilder.BuildResourceList<TemplateContext, Template, TemplateList>(contexts, ResponseBuilder.BuildTemplate, null);
            return Ok(resp);
        }
        #endregion

        #region DeleteTemplate
        /// <summary>
        /// Delete a hackathon template. 
        /// </summary>
        /// <remarks>
        /// Make sure delete all experiments created using it first.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="templateId" example="1009bb6-be04-4ba1-901b-21e2b5b0f714">Auto-generated Id of the template. Clients can get the Id in a Create, Update or List request.</param>
        /// <response code="204">Success. The response indicates that a template is successfully deleted.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/template/{templateId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteTemplate(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string templateId,
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

            // validate template
            var experiments = await ExperimentManagement.ListExperimentsAsync(hackathon, templateId, cancellationToken);
            if (experiments.Count() > 0)
            {
                return PreconditionFailed(Resources.Template_HasExperiment);
            }

            // delete template
            var context = await ExperimentManagement.DeleteTemplateAsync(hackathonName.ToLower(), templateId, cancellationToken);
            if (context?.TemplateEntity != null)
            {
                var logsArgs = new
                {
                    hackathonName = hackathon.DisplayName,
                    adminName = CurrentUserDisplayName,
                    templateName = context.TemplateEntity.DisplayName,
                };
                await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                     ActivityLogType.deleteTemplate, logsArgs, cancellationToken);
            }

            if (context?.Status != null && context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: context.TemplateEntity?.Id);
            }
            else
            {
                return NoContent();
            }
        }
        #endregion

        #region CreateExperiment
        /// <summary>
        /// Create a hackathon experiment. 
        /// Every enrolled user can create a predefined experiment for free.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The award</returns>
        /// <response code="200">Success. The response describes a experiment.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Experiment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/experiment")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> CreateExperiment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Experiment parameter,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate enrollment
            var enrollment = await EnrollmentManagement.GetEnrollmentAsync(hackathonName.ToLower(), CurrentUserId, cancellationToken);
            var enrollmentOptions = new ValidateEnrollmentOptions
            {
                ApprovedRequired = true,
                HackathonName = hackathonName,
            };
            if (ValidateEnrollment(enrollment, enrollmentOptions) == false)
            {
                return enrollmentOptions.ValidateResult;
            }

            // validate template.
            var templateContext = await ExperimentManagement.GetTemplateAsync(hackathonName.ToLower(), parameter.templateId, cancellationToken);
            if (templateContext == null)
            {
                return NotFound(string.Format(Resources.Template_NotFound, parameter.templateId, hackathonName));
            }
            if (templateContext.Status.IsFailed())
            {
                return Problem(
                    statusCode: templateContext.Status.Code.Value,
                    detail: templateContext.Status.Message,
                    title: templateContext.Status.Reason,
                    instance: templateContext.TemplateEntity?.DisplayName);
            }

            // create experiment
            parameter.hackathonName = hackathonName.ToLower();
            parameter.userId = CurrentUserId;
            var experimentContext = await ExperimentManagement.CreateOrUpdateExperimentAsync(parameter, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.createExperiment.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = experimentContext?.Status?.Message,
            }, cancellationToken);

            // build resp
            var userInfo = await GetCurrentUserInfo(cancellationToken);
            if (experimentContext.Status.IsFailed())
            {
                return Problem(
                    statusCode: experimentContext.Status.Code.Value,
                    detail: experimentContext.Status.Message,
                    title: experimentContext.Status.Reason,
                    instance: experimentContext.ExperimentEntity.Id);
            }
            else
            {
                return Ok(ResponseBuilder.BuildExperiment(experimentContext, userInfo));
            }
        }
        #endregion

        #region GetConnections
        /// <summary>
        /// Get connection infos for Guacamole. Trusted app only. The connection infos contain username, password etc.
        /// They are required for remote connections. Clients like guacamole can connect to the experiments via these connections.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="experimentId" example="6129c741-87e5-4a78-8173-f80724a70aea">id of the experiment</param>
        /// <returns>A list of connections</returns>
        /// <response code="200">Success. The response describes a list of connection info.</response>
        [HttpGet]
        [ProducesResponseType(typeof(GuacamoleConnectionList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/experiment/{experimentId}/connections")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        [Authorize(Policy = AuthConstant.Policy.TrustedApp)]
        public async Task<object> GetConnections(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string experimentId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // query experiment
            var context = await ExperimentManagement.GetExperimentAsync(hackathonName.ToLower(), experimentId, cancellationToken);
            if (context?.ExperimentEntity == null)
            {
                return NotFound(Resources.Experiment_NotFound);
            }
            if (context.ExperimentEntity.UserId != CurrentUserId)
            {
                return Forbidden(Resources.Experiment_UserNotMatch);
            }

            // build resp
            if (context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: context.ExperimentEntity.Id);
            }
            else
            {
                TemplateContext template = await ExperimentManagement.GetTemplateAsync(
                    hackathonName.ToLower(),
                    context.ExperimentEntity.TemplateId,
                    cancellationToken);
                var conn = ResponseBuilder.BuildGuacamoleConnection(context, template);
                var list = new GuacamoleConnectionList
                {
                    value = new object[] {
                        conn
                    }
                };
                return Ok(list);
            }
        }
        #endregion

        #region GetExperiment
        /// <summary>
        /// Get experiment by experimentId. Can get experimentId from experiment list api. 
        /// Sensitive information like username/password/port of the remote connection will not be returned.
        /// Those info are open to trusted apps only by calling `hackathon/{hackathonName}/experiment/{experimentId}/connections`.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="experimentId" example="6129c741-87e5-4a78-8173-f80724a70aea">id of the experiment</param>
        /// <returns>an experiment.</returns>
        /// <response code="200">Success. The response describes an experiment.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Experiment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/experiment/{experimentId}")]
        public async Task<object> GetExperiment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string experimentId,
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

            // query experiment
            var context = await ExperimentManagement.GetExperimentAsync(hackathonName.ToLower(), experimentId, cancellationToken);
            if (context?.ExperimentEntity == null)
            {
                return NotFound(Resources.Experiment_NotFound);
            }

            // build resp
            if (context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: experimentId);
            }
            else
            {
                var userInfo = await UserManagement.GetUserByIdAsync(context.ExperimentEntity.UserId, cancellationToken);
                var resp = ResponseBuilder.BuildExperiment(context, userInfo);
                return Ok(resp);
            }
        }
        #endregion

        #region ResetExperiment
        /// <summary>
        /// Reset experiment by experimentId. Can get experimentId from experiment list api. 
        /// An experiment can be reset by owner or the administrators of the hackathon.
        /// The experiment in the backend will be deleted and re-created. 
        /// All data saved in the experiment will be lost and cannot be recovered. 
        /// Please backup your data if needed.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="experimentId" example="6129c741-87e5-4a78-8173-f80724a70aea">id of the experiment</param>
        /// <returns>an experiment.</returns>
        /// <response code="200">Success. The response describes an experiment.</response>
        [HttpPost]
        [ProducesResponseType(typeof(Experiment), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 409)]
        [Route("hackathon/{hackathonName}/experiment/{experimentId}/reset")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> ResetExperiment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string experimentId,
            CancellationToken cancellationToken)
        {
            // query experiment
            var context = await ExperimentManagement.GetExperimentAsync(hackathonName.ToLower(), experimentId, cancellationToken);
            if (context?.ExperimentEntity == null)
            {
                return NotFound(Resources.Experiment_NotFound);
            }
            bool allowed = context.ExperimentEntity.UserId == CurrentUserId;

            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                // if userId doesn't match, check whether it's an admin
                HackAdminRequird = !allowed,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // reset
            context = await ExperimentManagement.ResetExperimentAsync(hackathonName.ToLower(), experimentId, cancellationToken);
            if (context?.ExperimentEntity != null)
            {
                var logsArgs = new
                {
                    hackathonName = hackathon.DisplayName,
                    userName = CurrentUserDisplayName,
                    experiment = experimentId,
                };
                await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                     ActivityLogType.resetExperiment, logsArgs, cancellationToken);
            }

            // build resp
            if (context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: experimentId);
            }
            else
            {
                var userInfo = await UserManagement.GetUserByIdAsync(context.ExperimentEntity.UserId, cancellationToken);
                var resp = ResponseBuilder.BuildExperiment(context, userInfo);
                return Ok(resp);
            }
        }
        #endregion

        #region ListExperiments
        /// <summary>
        /// List experiments by hackthonName(required) and templateId(optional). 
        /// Sensitive information like username/password/port of the remote connections will not be returned.
        /// Those info are open to trusted apps only by calling `hackathon/{hackathonName}/experiment/{experimentId}/connections`.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="templateId" example="1009bb6-be04-4ba1-901b-21e2b5b0f714">Optional Id of the template. Clients can get the Template Id in a Template Create(PUT), Update(PATCH) or List(GET) request.</param>
        /// <returns>an experiment.</returns>
        /// <response code="200">Success. The response describes an experiment.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ExperimentList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/experiments")]
        public async Task<object> ListExperiments(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery, Guid] string templateId,
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

            // list templates
            var contexts = await ExperimentManagement.ListExperimentsAsync(hackathon, templateId, cancellationToken);
            var resp = await ResponseBuilder.BuildResourceListAsync<ExperimentContext, Experiment, ExperimentList>(
                contexts,
                async (context, token) =>
                {
                    var userInfo = await UserManagement.GetUserByIdAsync(context.ExperimentEntity?.UserId, token);
                    return ResponseBuilder.BuildExperiment(context, userInfo);
                },
                null);
            return Ok(resp);
        }
        #endregion

        #region DeleteExperiment
        /// <summary>
        /// Delete a hackathon experiment.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="experimentId" example="6129c741-87e5-4a78-8173-f80724a70aea">id of the experiment</param>
        /// <response code="204">Success. The response indicates that the experiment is successfully deleted.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/experiment/{experimentId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteExperiment(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required, Guid] string experimentId,
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

            // delete experiment
            var context = await ExperimentManagement.DeleteExperimentAsync(hackathonName.ToLower(), experimentId, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.deleteExperiment.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = experimentId,
            }, cancellationToken);

            if (context?.Status != null && context.Status.IsFailed())
            {
                return Problem(
                    statusCode: context.Status.Code.Value,
                    detail: context.Status.Message,
                    title: context.Status.Reason,
                    instance: experimentId);
            }
            else
            {
                return NoContent();
            }
        }
        #endregion
    }
}
