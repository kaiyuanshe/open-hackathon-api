using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
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
    public class AnnouncementController : HackathonControllerBase
    {
        #region CreateAnnouncement
        /// <summary>
        /// Add a new announcement.
        /// </summary>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The created announcement.</returns>
        /// <response code="200">Success. The response describes an announcement.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Announcement), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/announcement")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateAnnouncement(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, HttpPutPolicy] Announcement parameter,
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

            // create announcement
            Debug.Assert(hackathon != null);
            parameter.hackathonName = hackathon.Name;
            var entity = await AnnouncementManagement.Create(parameter, cancellationToken);

            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                ActivityLogType.createAnnouncement, args, cancellationToken);

            var resp = ResponseBuilder.BuildAnnouncement(entity);
            return Ok(resp);
        }
        #endregion
    }
}
