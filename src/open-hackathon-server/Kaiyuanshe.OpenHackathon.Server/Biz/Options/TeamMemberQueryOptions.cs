using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class TeamMemberQueryOptions : TableQueryOptions
    {
        public string TeamId { get; set; }
        public TeamMemberRole? Role { get; set; }
        public TeamMemberStatus? Status { get; set; }
    }
}
