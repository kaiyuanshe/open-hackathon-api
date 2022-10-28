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
        Announcement,
        Award,
        AwardAssignment,
        Claims,
        Enrollment,
        Hackathon,
        HackathonAdmin,
        Judge,
        Organizer,
        Questionnaire,
        RatingKind,
        Team,
        TeamMember,
        TeamWork,
        Token,
        User,
    }
}
