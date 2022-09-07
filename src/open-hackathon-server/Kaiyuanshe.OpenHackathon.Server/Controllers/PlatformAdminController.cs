using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    }
}
