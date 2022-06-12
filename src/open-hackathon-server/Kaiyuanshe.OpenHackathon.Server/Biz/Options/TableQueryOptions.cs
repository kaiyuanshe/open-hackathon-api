using Kaiyuanshe.OpenHackathon.Server.Models;

namespace Kaiyuanshe.OpenHackathon.Server.Biz.Options
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
}
