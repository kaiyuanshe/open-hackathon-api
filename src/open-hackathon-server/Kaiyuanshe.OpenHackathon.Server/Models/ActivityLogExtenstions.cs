using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using SmartFormat;
using System;
using System.Globalization;

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

        public static void GenerateMessage(this ActivityLogEntity entity, object args)
        {
            if (entity == null)
                return;

            var resourceKey = entity.GetResourceKey();
            Func<CultureInfo, string> messageByCulture = (culture) =>
            {
                try
                {
                    var messageFormat = Resources.ResourceManager.GetString(resourceKey, culture);
                    if (messageFormat == null)
                    {
                        return null;
                    }

                    return Smart.Format(messageFormat, args);
                }
                catch (Exception)
                {
                    // return null in case of any Exception
                    return null;
                }
            };

            foreach (var culture in CultureInfos.SupportedCultures)
            {
                entity.Messages[culture.Name] = messageByCulture(culture);
            }
        }
    }
}
