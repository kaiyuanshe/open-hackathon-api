using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports
{
    public class EnrollmentReportsJob : ReportsBaseJob<EnrollmentReportsJob.EnrollmentReport>
    {
        internal override ReportType ReportType => ReportType.enrollments;

        protected override async Task<IList<EnrollmentReport>> GenerateReport(HackathonEntity hackathon, CancellationToken token)
        {
            IList<EnrollmentReport> reports = new List<EnrollmentReport>();

            var filter = TableQueryHelper.PartitionKeyFilter(hackathon.Name);
            await StorageContext.EnrollmentTable.ExecuteQueryAsync(filter, async (enrollment) =>
            {
                var user = await UserManagement.GetUserByIdAsync(enrollment.UserId, token);
                if (user == null)
                    return;

                var record = new EnrollmentReport
                {
                    UserId = user.Id,
                    UserName = user.Username,
                    Nickname = user.Nickname,
                    FamilyName = user.FamilyName,
                    MiddleName = user.MiddleName,
                    GivenName = user.GivenName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Gender = user.Gender,
                    // hackathon info
                    HackathonName = hackathon.Name,
                    HackathonDisplayName = hackathon.DisplayName,
                    // enrollment
                    EnrollmentId = enrollment.RowKey,
                    EnrollmentStatus = enrollment.Status.ToString(),
                    Extensions = JsonConvert.SerializeObject(enrollment.Extensions),
                };
                reports.Add(record);

            }, null, null, token);

            return reports;
        }
        public class EnrollmentReport
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Nickname { get; set; }
            public string FamilyName { get; set; }
            public string MiddleName { get; set; }
            public string GivenName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Gender { get; set; }
            // hackathon info
            public string HackathonName { get; set; }
            public string HackathonDisplayName { get; set; }
            // enrollment
            public string EnrollmentId { get; set; }
            public string EnrollmentStatus { get; set; }
            public string Extensions { get; set; }
        }
    }
}
