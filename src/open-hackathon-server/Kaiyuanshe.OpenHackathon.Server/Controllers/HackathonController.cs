using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class HackathonController : HackathonControllerBase
    {
        #region ListHackathon
        /// <summary>
        /// List paginated hackathons. Optionally client can send 'Authorization' header and/or query strings. 
        /// See the parameters for more details.
        /// </summary>
        /// <param name="search">keyword to search in hackathon name, displayName or details. Do case-insensitive substring match only.</param>
        /// <param name="userId">optional id of user. If <b>userId</b> in query is not empty, will override the current user from <code>Authorization</code> header.</param>
        /// <param name="orderby">order by. Default to <b>createdAt</b>.</param>
        /// <param name="listType">type of list. Default to <b>online</b>. <br />
        /// <ul>
        /// <li><b>online</b>: list hackathons in online status only regardless of the userId;</li>
        /// <li><b>admin</b>: list hackathons in any status where user has admin access, either userId or Auth header is required, otherwise empty list returned;</li>
        /// <li><b>enrolled</b>: list enrolled hackathons of a user, either userId or <code>Authorization</code> header is required, otherwise empty list returned;</li>
        /// <li><b>fresh</b>: hackathons that are about to start;</li>
        /// <li><b>created</b>: hackathons that are created by specified user(either userId in query or user from access token.</li>
        /// </ul>
        /// </param>
        /// <returns>A list of hackathon.</returns>
        /// <response code="200">Success. The response describes a list of hackathon and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(HackathonList), 200)]
        [Route("hackathons")]
        public async Task<object> ListHackathon(
            [FromQuery] Pagination pagination,
            [FromQuery] string search,
            [FromQuery] string userId,
            [FromQuery] HackathonOrderBy? orderby,
            [FromQuery] HackathonListType? listType,
            CancellationToken cancellationToken)
        {
            var options = new HackathonQueryOptions
            {
                Pagination = pagination,
                OrderBy = orderby,
                Search = search,
                ListType = listType,
                UserId = CurrentUserId,
            };

            var userQueried = User;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                // override the id from Auth header.
                options.UserId = userId.Trim();
                userQueried = ClaimsHelper.NewClaimsPrincipal(userId, options.IsPlatformAdmin);
            }
            options.IsPlatformAdmin = await HackathonAdminManagement.IsPlatformAdmin(options.UserId, cancellationToken);

            var entities = await HackathonManagement.ListPaginatedHackathonsAsync(options, cancellationToken);
            var entityWithRoles = await HackathonManagement.ListHackathonRolesAsync(entities, userQueried, cancellationToken);

            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            routeValues.Add(nameof(search), search);
            if (orderby.HasValue)
            {
                routeValues.Add(nameof(orderby), orderby.Value);
            }
            if (listType.HasValue)
            {
                routeValues.Add(nameof(listType), listType.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, options.NextPage);

            return Ok(ResponseBuilder.BuildResourceList<HackathonEntity, HackathonRoles, Hackathon, HackathonList>(
                    entityWithRoles, ResponseBuilder.BuildHackathon, nextLink));
        }
        #endregion

        #region CheckNameAvailability
        /// <summary>
        /// Check the name availability.
        /// </summary>
        /// <remarks>
        /// Check if a hackathon is valid. <br />
        /// A name could be invalid because of invalid length, containing invalid charactors or already in use.
        /// Please choose a different name if not valid.
        /// </remarks>
        /// <param name="parameter">parameter including the name to check</param>
        /// <param name="cancellationToken"></param>
        /// <returns>availability and a reason if not available.</returns>
        [HttpPost]
        [Route("hackathon/checkNameAvailability")]
        [SwaggerErrorResponse(400, 401)]
        [ProducesResponseType(typeof(NameAvailability), StatusCodes.Status200OK)]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.LoginUser)]
        public async Task<object> CheckNameAvailability(
            [FromBody, Required] NameAvailability parameter,
            CancellationToken cancellationToken)
        {
            if (!Regex.IsMatch(parameter.name, ModelConstants.HackathonNamePattern))
            {
                return parameter.Invalid(Resources.Hackathon_Name_Invalid);
            }

            var entity = await HackathonManagement.GetHackathonEntityByNameAsync(parameter.name.ToLower(), cancellationToken);
            if (entity != null)
            {
                return parameter.AlreadyExists(Resources.Hackathon_Name_Taken);
            }

            return parameter.OK();
        }
        #endregion

        #region CreateOrUpdate
        /// <summary>
        /// Create or update hackathon. 
        /// 
        /// Else create a new hackathon.
        /// </summary>
        /// <remarks>
        /// If hackathon with the name {hackathonName} exists, will trigger an Update operation(the caller must be an admin of the hackathon in this case). 
        /// Otherwise a new hackathon will be created.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns></returns>
        /// <response code="200">Success. The response describes a hackathon.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 412)]
        [Route("hackathon/{hackathonName}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateOrUpdate(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Hackathon parameter,
            CancellationToken cancellationToken)
        {
            string nameLowercase = hackathonName.ToLower();
            parameter.name = nameLowercase;
            var entity = await HackathonManagement.GetHackathonEntityByNameAsync(nameLowercase, cancellationToken);
            if (entity != null)
            {
                return await UpdateInternal(entity, parameter, cancellationToken);
            }
            else
            {
                var canCreate = await HackathonManagement.CanCreateHackathonAsync(User, cancellationToken);
                if (!canCreate)
                {
                    return PreconditionFailed(Resources.Hackathon_CreateTooMany);
                }

                parameter.creatorId = CurrentUserId;
                var created = await HackathonManagement.CreateHackathonAsync(parameter, cancellationToken);

                var logArgs = new
                {
                    hackathonName = created.DisplayName,
                    userName = CurrentUserDisplayName,
                };
                await ActivityLogManagement.OnHackathonEvent(
                    nameLowercase,
                    CurrentUserId,
                    ActivityLogType.createHackathon,
                    logArgs,
                    cancellationToken);

                var roles = await HackathonManagement.GetHackathonRolesAsync(created, User, cancellationToken);
                return Ok(ResponseBuilder.BuildHackathon(created, roles));
            }
        }
        #endregion

        #region Update

        /// <summary>
        /// Update hackathon. Caller must be adminstrator of the hackathon. 
        /// </summary>
        /// <remarks>
        /// In an Update request, properties that are not included in request will remain unchanged.
        /// </remarks>
        /// <param name="parameter"></param>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns></returns>
        /// <response code="200">Success. The response describes a hackathon.</response>
        [HttpPatch]
        [ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 403, 404)]
        [Route("hackathon/{hackathonName}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> Update(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody] Hackathon parameter,
            CancellationToken cancellationToken)
        {
            string nameLowercase = hackathonName.ToLower();
            parameter.name = nameLowercase;
            var entity = await HackathonManagement.GetHackathonEntityByNameAsync(nameLowercase, cancellationToken);
            return await UpdateInternal(entity, parameter, cancellationToken);
        }

        private async Task<object> UpdateInternal(HackathonEntity entity, Hackathon parameter, CancellationToken cancellationToken)
        {
            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                HackathonName = parameter.name,
            };
            if (await ValidateHackathon(entity, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            var updated = await HackathonManagement.UpdateHackathonAsync(parameter, cancellationToken);
            var logArgs = new
            {
                hackathonName = updated.DisplayName,
                userName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(
                updated.Name,
                CurrentUserId,
                ActivityLogType.updateHackathon,
                logArgs,
                cancellationToken);
            var roles = await HackathonManagement.GetHackathonRolesAsync(entity, User, cancellationToken);
            return Ok(ResponseBuilder.BuildHackathon(updated, roles));
        }
        #endregion

        #region Get
        /// <summary>
        /// Query a hackathon by name. 
        /// If a hackathon is offline(also know as Deleted), it's visible to platform admins only.
        /// If a hackathon is in planning or pendingApproval state, it's visible to the hackathon admins only.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns></returns>
        /// <response code="200">Success. The response describes a hackathon.</response>
        [HttpGet]
        [ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}")]
        public async Task<object> Get(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            var entity = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            if (entity == null)
            {
                return NotFound(string.Format(Resources.Hackathon_NotFound, hackathonName));
            }

            var role = await HackathonManagement.GetHackathonRolesAsync(entity, User, cancellationToken);
            if (!entity.IsOnline() && (role == null || !role.isAdmin))
            {
                return NotFound(string.Format(Resources.Hackathon_NotFound, hackathonName));
            }

            return Ok(ResponseBuilder.BuildHackathon(entity, role));
        }
        #endregion

        #region Delete
        /// <summary>
        /// Delete a hackathon by name. 
        /// </summary>
        /// <remarks>
        /// A hackathon can be deleted by any of its administrators. <br />
        /// A hackathon cannot be deleted if any award is assigned. Clients need to delete all award assignments first.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns></returns>
        /// <response code="204">Success. The response indicates the hackathon is deleted.</response>
        [HttpDelete]
        [Route("hackathon/{hackathonName}")]
        [SwaggerErrorResponse(400, 412)]
        [Authorize(AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> Delete(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            var entity = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
            if (entity == null)
            {
                return NoContent();
            }

            var options = new ValidateHackathonOptions
            {
                HackAdminRequird = true,
                NoAwardAssignmentRequired = true,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(entity, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            await HackathonManagement.UpdateHackathonStatusAsync(entity, HackathonStatus.offline, cancellationToken);
            var args = new
            {
                userName = CurrentUserDisplayName,
                hackathonName = entity.DisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathonName.ToLower(), CurrentUserId, ActivityLogType.deleteHackathon, args, cancellationToken);
            return NoContent();
        }
        #endregion

        #region RequestPublish
        /// <summary>
        /// Send an request to publish a draft hackathon. The hackathon will go online after the request approved.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the hackathon</returns>
        [HttpPost]
        [Route("hackathon/{hackathonName}/requestPublish")]
        [SwaggerErrorResponse(400, 404, 412)]
        [ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> RequestPublish(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            HackathonEntity hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                UserId = CurrentUserId,
                HackAdminRequird = true,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }
            if (hackathon.Status == HackathonStatus.online)
            {
                return PreconditionFailed(string.Format(Resources.Hackathon_AlreadyOnline, hackathonName));
            }

            // update status
            hackathon = await HackathonManagement.UpdateHackathonStatusAsync(hackathon, HackathonStatus.pendingApproval, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.publishHackathon.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = hackathon.DisplayName,
            }, cancellationToken);

            // resp
            var roles = await HackathonManagement.GetHackathonRolesAsync(hackathon, User, cancellationToken);
            return Ok(ResponseBuilder.BuildHackathon(hackathon, roles));
        }
        #endregion

        #region Publish
        /// <summary>
        /// Approve and publish a hackathon. 
        /// </summary>
        /// <remarks>
        /// The hackathon will go online and become visible to everyone. <br />
        /// To ensure all published hackathons are of high quality, only administrators of the open hackathon platform can approve and publish a hackathon.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the hackathon</returns>
        [HttpPost]
        [Route("hackathon/{hackathonName}/publish")]
        [SwaggerErrorResponse(400, 404, 412)]
        [ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [Authorize(Policy = AuthConstant.Policy.PlatformAdministrator)]
        public async Task<object> Publish(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            HackathonEntity hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                UserId = CurrentUserId,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }

            // update status
            hackathon = await HackathonManagement.UpdateHackathonStatusAsync(hackathon, HackathonStatus.online, cancellationToken);
            var args = new
            {
                hackathonName = hackathon.DisplayName
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId, ActivityLogType.approveHackahton, args, cancellationToken);

            // resp
            var roles = await HackathonManagement.GetHackathonRolesAsync(hackathon, User, cancellationToken);
            return Ok(ResponseBuilder.BuildHackathon(hackathon, roles));
        }
        #endregion

        #region UpdateReadOnly
        /// <summary>
        /// Mark a hackathon as Readnly or not Readonly. 
        /// If a hackathon is Readonly, it cannot be updated any more. All PUT/PATCH/DELETE operations on hackathon and its teams, awards, templates etc are forbidden.
        /// The GET APIs are still available.  Open hackathon platform admins can call this API again to remove the Readonly mode.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the hackathon</returns>
        [HttpPost]
        [Route("hackathon/{hackathonName}/updateReadonly")]
        [SwaggerErrorResponse(400, 404)]
        [ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [Authorize(Policy = AuthConstant.Policy.PlatformAdministrator)]
        public async Task<object> UpdateReadonly(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery, Required] bool readOnly,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            HackathonEntity hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                WritableRequired = false,
                HackathonName = hackathonName,
                UserId = CurrentUserId,
            };
            if (await ValidateHackathon(hackathon, options) == false)
            {
                return options.ValidateResult;
            }

            // update status
            hackathon = await HackathonManagement.UpdateHackathonReadOnlyAsync(hackathon, readOnly, cancellationToken);
            await ActivityLogManagement.LogActivity(new ActivityLogEntity
            {
                ActivityLogType = ActivityLogType.archiveHackathon.ToString(),
                HackathonName = hackathonName.ToLower(),
                OperatorId = CurrentUserId,
                Message = hackathon.DisplayName,
            }, cancellationToken);

            // resp
            var roles = await HackathonManagement.GetHackathonRolesAsync(hackathon, User, cancellationToken);
            return Ok(ResponseBuilder.BuildHackathon(hackathon, roles));
        }
        #endregion
    }
}
