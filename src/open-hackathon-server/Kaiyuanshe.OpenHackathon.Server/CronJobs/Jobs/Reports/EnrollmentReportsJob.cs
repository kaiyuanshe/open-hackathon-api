using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports
{
    public class EnrollmentReportsJob : ReportsBaseJob
    {
        protected override string ReportName => "Enrollments";

        protected override async Task<IList<dynamic>> GenerateReport(HackathonEntity hackathon, CancellationToken token)
        {
            IList<dynamic> reports = new List<dynamic>();

            var filter = TableQueryHelper.PartitionKeyFilter(hackathon.Name);
            await StorageContext.EnrollmentTable.ExecuteQueryAsync(filter, async (enrollment) =>
            {
                var user = await StorageContext.UserTable.GetUserByIdAsync(enrollment.UserId, token);
                if (user == null)
                    return;

                dynamic record = new ExpandoObject();
                // user info
                record.UserId = user.Id;
                record.UserName = user.Username;
                record.Nickname = user.Nickname;
                record.FamilyName = user.FamilyName;
                record.MiddleName = user.MiddleName;
                record.GivenName = user.GivenName;
                record.Email = user.Email;
                record.Phone = user.Phone;
                record.Gender = user.Gender;
                record.Nickname = user.Nickname;
                // hackathon info
                record.HackathonName = hackathon.Name;
                record.HackathonDisplayName = hackathon.DisplayName;
                // enrollment
                record.EnrollmentId = enrollment.RowKey;
                record.EnrollmentStatus = enrollment.Status.ToString();
                record.Extensions = JsonConvert.SerializeObject(enrollment.Extensions);

                reports.Add(record);
            }, null, null, token);

            return reports;
        }
    }
}
