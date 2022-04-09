using Kaiyuanshe.OpenHackathon.Server.Helpers;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public static class ActivityLogExtenstions
    {
        public static string GetResourceKey(this ActivityLogType activityLogType)
        {
            var attr = activityLogType.GetCustomAttribute<MessageFormatAttribute>();
            return attr?.ResourceKey ?? string.Empty;
        }
    }
}
