using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger.Annotations;
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
    public class TemplateRepoController : HackathonControllerBase
    {
        #region CreateTemplateRepo
        /// <summary>
        /// Add a new TemplateRepo.
        /// </summary>
        /// <remarks>
        /// One hackathon provides one project template.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The created project template.</returns>
        /// <response code="200">Success. The response describes an project template.</response>
        [HttpPut]
        [ProducesResponseType(typeof(TemplateRepo), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/templateRepo/")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateTemplateRepo(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, HttpPutPolicy] TemplateRepo parameter,
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
            var entity = await TemplateRepoManagement.CreateTemplateRepoAsync(parameter, cancellationToken);

            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.createTemplateRepo, args, cancellationToken);

            var resp = ResponseBuilder.BuildTemplateRepo(entity);
            return Ok(resp);
        }
        #endregion

        #region UpdateTemplateRepo
        /// <summary>
        /// Update an project template.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The updated project template.</returns>
        /// <response code="200">Success. The response describes an project template.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(TemplateRepo), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/templateRepo/{templateRepoId}/")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> UpdateTemplateRepo(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Guid, Required] string templateRepoId,
            [FromBody, Required] TemplateRepo parameter,
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
            var entity = await TemplateRepoManagement.GetTemplateRepoAsync(hackathon.Name, templateRepoId, cancellationToken);
            if (entity == null)
            {
                return NotFound(Resources.TemplateRepo_NotFound);
            }
            entity = await TemplateRepoManagement.UpdateTemplateRepoAsync(entity, parameter, cancellationToken);

            // logs
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.updateTemplateRepo, args, cancellationToken);

            var resp = ResponseBuilder.BuildTemplateRepo(entity);
            return Ok(resp);
        }
        #endregion

        #region GetTemplateRepo
        /// <summary>
        /// Get an TemplateRepo by id.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The project template.</returns>
        /// <response code="200">Success. The response describes an project template.</response>
        [HttpGet]
        [ProducesResponseType(typeof(TemplateRepo), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/templateRepo/{templateRepoId}/")]
        public async Task<object> GetTemplateRepo(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Guid, Required] string templateRepoId,
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
            var entity = await TemplateRepoManagement.GetTemplateRepoAsync(hackathon.Name, templateRepoId, cancellationToken);
            if (entity == null)
            {
                return NotFound(Resources.TemplateRepo_NotFound);
            }
            var resp = ResponseBuilder.BuildTemplateRepo(entity);
            return Ok(resp);
        }
        #endregion

        #region ListByHackathon
        /// <summary>
        /// List paginated TemplateRepos of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>the response contains a list of judges and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of TemplateRepos and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(OrganizerList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/templateRepo/")]
        public async Task<object> ListByHackathon(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }

            // query
            var queryOptions = new TemplateRepoQueryOptions
            {
                Pagination = pagination,
            };
            var templateRepos = await TemplateRepoManagement.ListPaginatedTemplateReposAsync(hackathonName.ToLower(), queryOptions, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, queryOptions.NextPage);

            // build resp
            var resp = ResponseBuilder.BuildResourceList<TemplateRepoEntity, TemplateRepo, TemplateRepoList>(
                templateRepos,
                ResponseBuilder.BuildTemplateRepo,
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region DeleteTemplateRepo
        /// <summary>
        /// Delete an project template.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <response code="204">Deleted</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/templateRepo/")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteTemplateRepo(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Guid, Required] string templateRepoId,
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
            var entity = await TemplateRepoManagement.GetTemplateRepoAsync(hackathon.Name, templateRepoId, cancellationToken);
            if (entity == null)
            {
                return NoContent();
            }

            await TemplateRepoManagement.DeleteTemplateRepoAsync(entity.HackathonName, templateRepoId, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.deleteTemplateRepo, args, cancellationToken);
            return NoContent();
        }
        #endregion
    }
}
