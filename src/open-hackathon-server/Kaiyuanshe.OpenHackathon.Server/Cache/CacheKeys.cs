namespace Kaiyuanshe.OpenHackathon.Server.Cache
{
    public class CacheKeys
    {
        public static string GetCacheKey(CacheEntryType cacheEntryType, string subCacheKey)
        {
            return $"{cacheEntryType}-{subCacheKey}";
        }
    }

    public enum CacheEntryType
    {
        Default,
        Hackathon,
        Token,
        Claims,
        HackathonAdmin,
        User,
        Enrollment,
        Award,
        AwardAssignment,
        Team,
        TeamMember,
        TeamWork,
        Judge,
        RatingKind,
        Organizer,
    }
}
