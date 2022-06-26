using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class OrganizerController : HackathonControllerBase
    {
        #region CreateOrganizer
        /// <summary>
        /// Add a new organizer.
        /// </summary>
        /// <remarks>
        /// Add a company/organization who organizes the event. 
        /// Could a host, organizer, co-organizer, sponsor or title sponsor.
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <returns>The created organizer.</returns>
        /// <response code="200">Success. The response describes an organizer.</response>
        [HttpPut]
        [ProducesResponseType(typeof(Organizer), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/organizer")]
        [Authorize(Policy = AuthConstant.PolicyForSwagger.HackathonAdministrator)]
        public async Task<object> CreateOrganizer(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromBody, HttpPutPolicy] Organizer parameter,
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

            // create organizer
            parameter.hackathonName = hackathonName.ToLower();
            var organizerEntity = await OrganizerManagement.CreateOrganizer(hackathonName.ToLower(), parameter, cancellationToken);

            var args = new
            {
                hackathonName = hackathon.DisplayName,
                adminName = CurrentUserDisplayName,
                organizerName = parameter.name,
            };
            await ActivityLogManagement.OnHackathonEvent(hackathon.Name, CurrentUserId,
                ActivityLogType.createOrganizer, args, cancellationToken);

            var resp = ResponseBuilder.BuildOrganizer(organizerEntity);
            return Ok(resp);
        }
        #endregion
    }
}
