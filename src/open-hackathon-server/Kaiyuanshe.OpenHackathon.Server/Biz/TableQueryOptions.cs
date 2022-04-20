using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public class TableQueryOptions
    {
        public Pagination Pagination { get; set; }

        public Pagination NextPage { get; set; }
    }

    public static class TableQueryOptionsExtension
    {
        public static int Top(this TableQueryOptions options, int defaultValue = 100)
        {
            if (options?.Pagination?.top != null && options.Pagination.top.Value > 0)
            {
                return options.Pagination.top.Value;
            }

            return defaultValue;
        }

        public static string ContinuationToken(this TableQueryOptions options)
        {
            if (options?.Pagination != null)
                return options.Pagination.ToContinuationToken();

            return null;
        }
    }

    public class AdminQueryOptions : TableQueryOptions
    {

    }

    public class EnrollmentQueryOptions : TableQueryOptions
    {
        public EnrollmentStatus? Status { get; set; }
    }

    public class TeamQueryOptions : TableQueryOptions
    {

    }

    public class TeamMemberQueryOptions : TableQueryOptions
    {
        public TeamMemberRole? Role { get; set; }
        public TeamMemberStatus? Status { get; set; }
    }

    public class AwardQueryOptions : TableQueryOptions
    {

    }

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

    public class TeamWorkQueryOptions : TableQueryOptions
    {

    }

    public class JudgeQueryOptions : TableQueryOptions
    {

    }

    public class RatingKindQueryOptions : TableQueryOptions
    {

    }

    public class RatingQueryOptions : TableQueryOptions
    {
        public string TeamId { get; set; }
        public string RatingKindId { get; set; }
        public string JudgeId { get; set; }
    }

    public class UserQueryOptions
    {
        public string Search { get; set; }
        public int Top { get; set; } = 100;
    }
}
