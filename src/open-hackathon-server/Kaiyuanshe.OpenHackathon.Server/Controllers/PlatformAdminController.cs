using Kaiyuanshe.OpenHackathon.Server.Auth;
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
    public class PlatformAdminController : HackathonControllerBase
    {
        #region CreatePlatformAdmin
        /// <summary>
        /// Add an user as platform admin. 
        /// </summary>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The created or updated admin.</returns>
        /// <response code="200">Success. The response describes an admin.</response>
        [HttpPut]
        [ProducesResponseType(typeof(HackathonAdmin), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("platform/admin/{userId}")]
        [Authorize(Policy = AuthConstant.Policy.PlatformAdministrator)]
        public async Task<object> CreatePlatformAdmin(
            [FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            // validate user
            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(Resources.User_NotFound);
            }

            // create admin
            var admin = new HackathonAdmin
            {
                hackathonName = string.Empty,
                userId = userId,
            };
            var adminEntity = await HackathonAdminManagement.CreateAdminAsync(admin, cancellationToken);

            Debug.Assert(user != null);
            var args = new
            {
                userName = user.GetDisplayName(),
                operatorName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnUserEvent(string.Empty, adminEntity.UserId, CurrentUserId,
                ActivityLogType.createPlatformAdmin, args, null, nameof(Resources.ActivityLog_User_createPlatformAdmin2), cancellationToken);

            var resp = ResponseBuilder.BuildHackathonAdmin(adminEntity, user);
            return Ok(resp);
        }
        #endregion

        #region DeletePlatformAdmin
        /// <summary>
        /// Remove a platform admin. 
        /// </summary>
        /// <param name="userId" example="1">Id of user</param>
        /// <returns>The created or updated admin.</returns>
        /// <response code="204">Success. The response indicates the platform admin is deleted.</response>
        [HttpDelete]
        [SwaggerErrorResponse(400, 404, 412)]
        [Route("platform/admin/{userId}")]
        [Authorize(Policy = AuthConstant.Policy.PlatformAdministrator)]
        public async Task<object> DeletePlatformAdmin(
            [FromRoute, Required] string userId,
            CancellationToken cancellationToken)
        {
            // validate user
            var user = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(Resources.User_NotFound);
            }

            // create admin
            await HackathonAdminManagement.DeleteAdminAsync(string.Empty, userId, cancellationToken);

            var args = new
            {
                userName = user.GetDisplayName(),
                operatorName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnUserEvent(string.Empty, userId, CurrentUserId,
                ActivityLogType.deletePlatformAdmin, args, nameof(Resources.ActivityLog_User_deletePlatformAdmin2), nameof(Resources.ActivityLog_User_deletePlatformAdmin), cancellationToken);

            return NoContent();
        }
        #endregion

        #region ListPlatformAdmins
        /// <summary>
        /// List paginated admins of the open hackathon platform.
        /// </summary>
        /// <returns>the response contains a list of admins and a nextLink if there are more results.</returns>
        /// <response code="200">Success. The response describes a list of admin user and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(HackathonAdminList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Authorize(Policy = AuthConstant.Policy.PlatformAdministrator)]
        [Route("platform/admins")]
        public async Task<object> ListPlatformAdmins(
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            // query
            var queryOptioins = new AdminQueryOptions
            {
                Pagination = pagination,
            };
            var admins = await HackathonAdminManagement.ListPaginatedHackathonAdminAsync(string.Empty, queryOptioins, cancellationToken);
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
    }
}
