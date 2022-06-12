namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class AwardAssignmentQueryOptions : TableQueryOptions
    {
        public AwardAssignmentQueryType QueryType { get; set; }
        public string AwardId { get; set; }
        public string TeamId { get; set; }
    }

    public enum AwardAssignmentQueryType
    {
        Award,
        Team,
        Hackathon,
    }
}
