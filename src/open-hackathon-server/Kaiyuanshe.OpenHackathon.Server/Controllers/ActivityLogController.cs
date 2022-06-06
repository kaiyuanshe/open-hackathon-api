using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class ActivityLogController : HackathonControllerBase
    {
        #region ListAcitivitiesByHackathon
        /// <summary>
        /// View activities of a hackathon.
        /// </summary>
        /// <remarks>
        /// Multiple languages are supported.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A list of activitiy logs.</returns>
        /// <response code="200">Success. The response describes a list of activity logs and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ActivityLogList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(404)]
        [Route("hackathon/{hackathonName}/activityLogs")]
        public async Task<object> ListAcitivitiesByHackathon(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower());
            var options = new ValidateHackathonOptions
            {
                HackathonName = hackathonName,
                WritableRequired = false,
            };
            if (await ValidateHackathon(hackathon, options, cancellationToken) == false)
            {
                return options.ValidateResult;
            }

            // query logs
            var logOptions = new ActivityLogQueryOptions
            {
                Pagination = pagination,
                HackathonName = hackathon.Name,
                Category = ActivityLogCategory.Hackathon,
            };
            var logs = await ActivityLogManagement.ListActivityLogs(logOptions, cancellationToken);

            // resp
            var nextLink = BuildNextLinkUrl(null, logOptions.NextPage);
            var resp = ResponseBuilder.BuildResourceList<ActivityLogEntity, ActivityLog, ActivityLogList>(logs, ResponseBuilder.BuildActivityLog, nextLink);
            return Ok(resp);
        }
        #endregion

        #region ListAcitivitiesByUser
        /// <summary>
        /// View activities of a user.
        /// </summary>
        /// <remarks>
        /// Multiple languages are supported.
        /// </remarks>
        /// <param name="userId" example="1">unique id of the user</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A list of activitiy logs.</returns>
        /// <response code="200">Success. The response describes a list of activity logs and a nullable link to query more results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ActivityLogList), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(404)]
        [Route("user/{userId}/activityLogs")]
        public async Task<object> ListAcitivitiesByUser(
            [FromRoute, Required] string userId,
            [FromQuery] Pagination pagination,
            CancellationToken cancellationToken)
        {
            var userInfo = await UserManagement.GetUserByIdAsync(userId, cancellationToken);
            if (userInfo == null)
            {
                return NotFound(Resources.User_NotFound);
            }

            // query logs
            var options = new ActivityLogQueryOptions
            {
                Pagination = pagination,
                UserId = userId,
                Category = ActivityLogCategory.User,
            };
            var logs = await ActivityLogManagement.ListActivityLogs(options, cancellationToken);

            // resp
            var nextLink = BuildNextLinkUrl(null, options.NextPage);
            var resp = ResponseBuilder.BuildResourceList<ActivityLogEntity, ActivityLog, ActivityLogList>(logs, ResponseBuilder.BuildActivityLog, nextLink);
            return Ok(resp);
        }
        #endregion
    }
}
