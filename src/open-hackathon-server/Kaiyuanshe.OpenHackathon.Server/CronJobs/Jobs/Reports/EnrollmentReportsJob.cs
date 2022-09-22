using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports
{
    public class EnrollmentReportsJob : ReportsBaseJob
    {
        protected override string ReportName => "Enrollments";

        protected override Task<IList<dynamic>> GenerateReport(HackathonEntity hackathon, CancellationToken token)
        {
            IList<dynamic> reports = new List<dynamic>();

            return Task.FromResult(reports);
        }
    }
}
