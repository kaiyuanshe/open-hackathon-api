using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;

namespace Kaiyuanshe.OpenHackathon.Server.Models
{
    public static class ActivityLogExtenstions
    {
        private static readonly string ResourceKeyPrefix = "ActivityLog";

        public static string GetResourceKey(this ActivityLogEntity entity)
        {
            if (entity == null)
                return null;

            return $"{ResourceKeyPrefix}_{entity.Category}_{entity.ActivityLogType}";
        }

        public static string GetMessage(this ActivityLogEntity entity)
        {
            if (entity == null)
                return null;

            try
            {
                var logType = Enum.Parse<ActivityLogType>(entity.ActivityLogType);
                var resourceKey = entity.GetResourceKey();
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
