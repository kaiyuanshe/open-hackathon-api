using Kaiyuanshe.OpenHackathon.Server.Helpers;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public static class ActivityLogExtenstions
    {
        public static string GetResourceKey(this ActivityLogType activityLogType)
        {
            var attr = activityLogType.GetCustomAttribute<MessageFormatAttribute>();
            return attr?.ResourceKey ?? string.Empty;
        }

        public static string GetMessage(this ActivityLogEntity entity)
        {
            if (entity == null)
                return null;

            try
            {
                var logType = Enum.Parse<ActivityLogType>(entity.ActivityLogType);
                var resourceKey = GetResourceKey(logType);
                var messageFormat = Resources.ResourceManager.GetString(resourceKey);
                return string.Format(messageFormat, entity.Args);
            }
            catch (Exception)
            {
                // return default message in case of any Exception
                return entity.Message;
            }
        }
    }
}
