using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports
{
    public class TeamWorkReportJob : ReportsBaseJob<TeamWorkReportJob.TeamWorkReport>
    {
        internal override ReportType ReportType => ReportType.teamWorks;

        protected override async Task<IList<TeamWorkReport>> GenerateReport(HackathonEntity hackathon, CancellationToken token)
        {
            List<TeamWorkReport> reports = new List<TeamWorkReport>();

            var filter = TableQueryHelper.PartitionKeyFilter(hackathon.Name);
            var teams = await StorageContext.TeamTable.QueryEntitiesAsync(filter, null, token);
            await StorageContext.TeamWorkTable.ExecuteQueryAsync(filter, async (work) =>
            {
                var team = teams.FirstOrDefault(t => t.Id == work.TeamId);
                if (team == null)
                    return;

                var creator = await UserManagement.GetUserByIdAsync(team.CreatorId, token);
                if (creator == null)
                    return;

                var report = new TeamWorkReport
                {
                    // hack
                    HackathonName = hackathon.Name,
                    HackathonDisplayName = hackathon.DisplayName,
                    // team
                    TeamAutoApprove = team.AutoApprove,
                    TeamCreatorId = creator.Id,
                    TeamCreatorUserName = creator.Username ?? creator.Name,
                    TeamDescription = team.Description,
                    TeamId = team.Id,
                    TeamMemberCount = team.MembersCount,
                    TeamName = team.DisplayName,
                    // work
                    WorkId = work.Id,
                    WorkTitle = work.Title,
                    WorkDescription = work.Description,
                    WorkType = work.Type.ToString(),
                    WorkUrl = work.Url,
                };
                reports.Add(report);
            }, null, null, token);

            return reports;
        }

        public class TeamWorkReport
        {
            // hackathon info
            public string HackathonName { get; set; }
            public string HackathonDisplayName { get; set; }
            // team info
            public string TeamId { get; set; }
            public string TeamName { get; set; }
            public string TeamDescription { get; set; }
            public bool TeamAutoApprove { get; set; }
            public string TeamCreatorId { get; set; }
            public string TeamCreatorUserName { get; set; }
            public int TeamMemberCount { get; set; }
            // work
            public string WorkId { get; set; }

            public string WorkTitle { get; set; }

            public string WorkDescription { get; set; }

            public string WorkType { get; set; }

            public string WorkUrl { get; set; }
        }
    }
}
