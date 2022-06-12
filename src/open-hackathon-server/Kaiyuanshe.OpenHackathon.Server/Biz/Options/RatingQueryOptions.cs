namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
{
    public class RatingQueryOptions : TableQueryOptions
    {
        public string TeamId { get; set; }
        public string RatingKindId { get; set; }
        public string JudgeId { get; set; }
    }
}
