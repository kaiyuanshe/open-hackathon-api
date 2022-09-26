using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports
{
    public class TeamMemberReportJob : ReportsBaseJob<TeamMemberReportJob.TeamMemberReport>
    {
        internal override ReportType ReportType => ReportType.teams;

        protected override async Task<IList<TeamMemberReport>> GenerateReport(HackathonEntity hackathon, CancellationToken token)
        {
            List<TeamMemberReport> reports = new List<TeamMemberReport>();

            var filter = TableQueryHelper.PartitionKeyFilter(hackathon.Name);
            var teams = await StorageContext.TeamTable.QueryEntitiesAsync(filter, null, token);
            await StorageContext.TeamMemberTable.ExecuteQueryAsync(filter, async (member) =>
            {
                var team = teams.FirstOrDefault(t => t.Id == member.TeamId);
                if (team == null)
                    return;

                var user = await UserManagement.GetUserByIdAsync(member.UserId, token);
                if (user == null)
                    return;

                var creator = await UserManagement.GetUserByIdAsync(team.CreatorId, token);
                if (creator == null)
                    return;

                var report = new TeamMemberReport
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
                    // member
                    MemberDescription = member.Description,
                    MemberEmail = user.Email,
                    MemberFamilyName = user.FamilyName,
                    MemberGender = user.Gender,
                    MemberGivenName = user.GivenName,
                    MemberId = member.UserId,
                    MemberMiddleName = user.MiddleName,
                    MemberNickname = user.Nickname,
                    MemberPhone = user.Phone,
                    MemberRole = member.Role.ToString(),
                    MemberStatus = member.Status.ToString(),
                    MemberUserName = user.Username ?? user.Name
                };
                reports.Add(report);
            }, null, null, token);

            return reports;
        }

        public class TeamMemberReport
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
            // member info
            public string MemberId { get; set; }
            public string MemberUserName { get; set; }
            public string MemberNickname { get; set; }
            public string MemberFamilyName { get; set; }
            public string MemberMiddleName { get; set; }
            public string MemberGivenName { get; set; }
            public string MemberEmail { get; set; }
            public string MemberPhone { get; set; }
            public string MemberGender { get; set; }
            public string MemberDescription { get; set; }
            public string MemberRole { get; set; }
            public string MemberStatus { get; set; }
        }
    }
}
