using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class TeamMemberQueryOptions : TableQueryOptions
    {
        public TeamMemberRole? Role { get; set; }
        public TeamMemberStatus? Status { get; set; }
    }
}
