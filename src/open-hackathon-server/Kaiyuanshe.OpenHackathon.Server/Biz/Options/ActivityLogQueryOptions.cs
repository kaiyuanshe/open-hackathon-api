namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class ActivityLogQueryOptions : TableQueryOptions
    {
        public string HackathonName { get; set; }
        public string UserId { get; set; }
    }
}
