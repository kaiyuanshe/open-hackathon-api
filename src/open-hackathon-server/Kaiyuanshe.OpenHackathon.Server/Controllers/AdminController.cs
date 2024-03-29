﻿using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
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
    public class AdminController : HackathonControllerBase
    {
        #region CreateAdmin
        /// <summary>
        /// Add an user as hackathon admin.
        /// </summary>
        /// <remarks>
        /// The creator of a hackathon is added as admin by default, don't have to add again. <br />
        /// Can use this API to update an existing admin by passing in the same userId.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The created or updated admin.</returns>
        /// <response code="200">Success. The response describes an admin.</response>
        [HttpPut]
        [ProducesResponseType(typeof(HackathonAdmin), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/admin/{userId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateAdmin(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
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

            // validate user
            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(Resources.User_NotFound);
            }

            // create admin
            var admin = new HackathonAdmin
            {
                hackathonName = hackathonName.ToLower(),
                userId = userId,
            };
            var adminEntity = await HackathonAdminManagement.CreateAdminAsync(admin, cancellationToken);

            var updatedUser = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(updatedUser != null);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                userName = CurrentUserDisplayName,
                adminName = updatedUser.GetDisplayName(),
            };
            await ActivityLogManagement.OnUserEvent(hackathon.Name, adminEntity.UserId, CurrentUserId,
                ActivityLogType.createHackathonAdmin, args, nameof(Resources.ActivityLog_User_createHackathonAdmin2), null, cancellationToken);

            var resp = ResponseBuilder.BuildHackathonAdmin(adminEntity, user);
            return Ok(resp);
        }
        #endregion

        #region ListAdmins
        /// <summary>
        /// List paginated admins of a hackathon.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>the response contains a list of admins and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of admin user and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(HackathonAdminList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/admins")]
        public async Task<object> ListAdmins(
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
            var queryOptioins = new AdminQueryOptions
            {
                Pagination = pagination,
            };
            var admins = await HackathonAdminManagement.ListPaginatedHackathonAdminAsync(hackathonName.ToLower(), queryOptioins, cancellationToken);
            var routeValues = new RouteValueDictionary();
            if (pagination.top.HasValue)
            {
                routeValues.Add(nameof(pagination.top), pagination.top.Value);
            }
            var nextLink = BuildNextLinkUrl(routeValues, queryOptioins.NextPage);

            // build resp
            var resp = await ResponseBuilder.BuildResourceListAsync<HackathonAdminEntity, HackathonAdmin, HackathonAdminList>(
                admins,
                async (admin, ct) =>
                {
                    var userInfo = await UserManagement.GetUserByIdAsync(admin.UserId, ct);
                    Debug.Assert(userInfo != null);
                    return ResponseBuilder.BuildHackathonAdmin(admin, userInfo);
                },
                nextLink);

            return Ok(resp);
        }
        #endregion

        #region GetAdmin
        /// <summary>
        /// query an admin.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The admin</returns>
        /// <response code="200">Success. The response describes an admin.</response>
        [HttpGet]
        [ProducesResponseType(typeof(HackathonAdmin), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/admin/{userId}")]
        public async Task<object> GetAdmin(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            var options = new ValidateHackathonOptions
            {
                WritableRequired = false,
                HackathonName = hackathonName,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // validate user
            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(Resources.User_NotFound);
            }

            // create admin
            var adminEntity = await HackathonAdminManagement.GetAdminAsync(hackathonName.ToLower(), userId, cancellationToken);
            if (adminEntity == null)
            {
                return NotFound(Resources.Admin_NotFound);
            }
            var resp = ResponseBuilder.BuildHackathonAdmin(adminEntity, user);
            return Ok(resp);
        }
        #endregion

        #region DeleteAdmin
        /// <summary>
        /// Delete a hackathon admin.
        /// </summary>
        /// <remarks>
        /// Delete a hackathon administrator. Note that the creator of the hackathon cannot be deleted.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="userId" example="1">Id of user</param>
        /// <response code="204">Success.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("hackathon/{hackathonName}/admin/{userId}")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> DeleteAdmin(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromRoute, Required] string userId,
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

            // get admin
            var adminEntity = await HackathonAdminManagement.GetAdminAsync(hackathonName.ToLower(), userId, cancellationToken);
            if (adminEntity == null)
            {
                return NoContent();
            }

            if (hackathon.CreatorId == userId)
            {
                return PreconditionFailed(Resources.Admin_CannotDeleteCreator);
            }

            await HackathonAdminManagement.DeleteAdminAsync(hackathonName.ToLower(), userId, cancellationToken);
            var deletedUser = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            Debug.Assert(deletedUser != null);
            var args = new
            {
                hackathonName = hackathon.DisplayName,
                userName = CurrentUserDisplayName,
                adminName = deletedUser.GetDisplayName(),
            };
            await ActivityLogManagement.OnUserEvent(hackathon.Name, adminEntity.UserId, CurrentUserId,
                ActivityLogType.deleteHackathonAdmin, args,
                nameof(Resources.ActivityLog_User_deleteHackathonAdmin2), null, cancellationToken);
            return NoContent();
        }
        #endregion
    }
}
