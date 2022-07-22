using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class ActivityLogQueryOptions : TableQueryOptions
    {
        public ActivityLogCategory Category { get; set; }
        public string TeamId { get; set; }
        public string UserId { get; set; }
    }
}
