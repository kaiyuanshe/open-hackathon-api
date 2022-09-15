using CsvHelper;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Swagger;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Controllers
{
    public class ReportController : HackathonControllerBase
    {

        #region GetReport
        ///// <summary>
        ///// Query a hackathon by name. 
        ///// If a hackathon is offline(also know as Deleted), it's visible to platform admins only.
        ///// If a hackathon is in planning or pendingApproval state, it's visible to the hackathon admins only.
        ///// </summary>
        ///// <param name="hackathonName" example="foo">Name of hackathon. Case-insensitive.
        ///// Must contain only letters and/or numbers, length between 1 and 100</param>
        ///// <returns></returns>
        ///// <response code="200">Success. The response describes a hackathon.</response>
        [HttpGet]
        //[ProducesResponseType(typeof(Hackathon), StatusCodes.Status200OK)]
        [SwaggerErrorResponse(400, 404)]
        //[Route("hackathon/{hackathonName}/report")]
        public async Task<object> GetReport(
            [FromRoute, Required, RegularExpression(ModelConstants.HackathonNamePattern)] string hackathonName,
            CancellationToken cancellationToken)
        {
            var entity = await HackathonManagement.GetHackathonEntityByNameAsync(hackathonName.ToLower(), cancellationToken);
            if (entity == null)
            {
                return NotFound(string.Format(Resources.Hackathon_NotFound, hackathonName));
            }

            var records = new List<dynamic>();
            dynamic record = new ExpandoObject();
            record.Id = 1;
            record.Name = "one";
            records.Add(record);

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            HttpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=myfilename.csv");

            return File(stream.ToArray(), "text/csv", "test.csv");
        }
        #endregion 
    }
}
