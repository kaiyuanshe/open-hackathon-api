using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class ReportController : HackathonControllerBase
    {
        #region GetReport
        /// <summary>
        /// Download hackathon data as file.
        /// </summary>
        /// <remarks>
        /// Download hackathon data as file:
        /// <ul>
        ///     <li>Only available for hackathon admins. There are two ways for this API: 
        ///         use the standard <b>Authorization</b> header or append access <b>token</b> to query string.</li>
        ///     <li>Only support CSV format for now.</li>
        ///     <li>There might be a delay(up to 18 hours) for new changes. 
        ///         Since the backend will re-generate the reports every 18 hours 
        ///         and this rest api only returns the latest reports that are already generated.
        ///         Please download again after 24 hours.</li>
        ///     <li>If 404 Not Found is returned, wait for 24 hours and retry. 
        ///         It just means the no report is ready and usually happens to a brand new hackathon.</li>
        /// </ul>
        /// Note that 
        /// </remarks>
        /// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        /// Must contain only letters and/or numbers, length between 1 and 100</param>
        /// <param name="reportType">type of report. required. See below for supported type <br />
        /// <ul>
        /// <li><b>enrollments</b>: export all enrolled users</li>
        /// </ul>
        /// </param>
        /// <param name="token">access token from login. It's optional but either Authorization or token is required. 
        /// If both are set, the token in query takes precedence.</param>
        /// <returns></returns>
        /// <response code="200">Success. The response is a file.</response>
        [HttpGet]
        [SwaggerErrorResponse(400, 404)]
        [Route("hackathon/{hackathonName}/report")]
        public async Task<object> GetReport(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            [FromQuery] ReportType reportType,
            [FromQuery] string? token,
            CancellationToken cancellationToken)
        {
            // validate hackathon
            var hackathon = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            if (hackathon == null)
            {
                return NotFound(string.Format(Resources.Hackathon_NotFound, hackathonName));
            }

            // validate token
            var usr = User;
            if (token != null)
            {
                var claims = await UserManagement.GetUserBasicClaimsAsync(token, cancellationToken);
                usr = ClaimsHelper.NewClaimsPrincipal(claims);
            }
            var authorizationResult = await AuthorizationService.AuthorizeAsync(usr, hackathon, AuthConstant.Policy.HackathonAdministrator);
            if (!authorizationResult.Succeeded)
            {
                return Forbidden(Resources.Hackathon_NotAdmin);
            }

            // get report
            var bytes = await FileManagement.DownloadReport(hackathonName.ToLower(), reportType, cancellationToken);
            if (bytes == null)
            {
                return NotFound(Resources.HackathonReport_NotFound);
            }

            var fileName = $"{hackathonName.ToLower()}-{reportType}.csv";
            HttpContext.Response.Headers.Add(HeaderNames.ContentDisposition, $"attachment;filename={fileName}");
            return File(bytes, "text/csv", fileName);
        }
        #endregion 
    }
}
