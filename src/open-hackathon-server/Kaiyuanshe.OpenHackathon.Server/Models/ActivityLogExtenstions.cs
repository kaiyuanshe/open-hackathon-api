using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.OpenApi.Writers;
using SmartFormat;
using System;
using System.Globalization;
using System.Linq;

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

        public static void GenerateMessage(this ActivityLogEntity entity, object? args, string? resourceKey = null)
        {
            if (entity == null)
                return;

            resourceKey ??= entity.GetResourceKey();
            entity.MessageResourceKey = resourceKey;
            Func<CultureInfo, string?> messageByCulture = (culture) =>
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

        public static string? GetMessage(this ActivityLogEntity entity)
        {
            if (entity == null)
                return null;

            var cultureInfo = CultureInfo.CurrentUICulture ?? CultureInfos.en_US;
            if (entity.Messages.ContainsKey(cultureInfo.Name))
            {
                return entity.Messages[cultureInfo.Name];
            }

            return entity.Messages.Values.FirstOrDefault(m => m != null);
        }

        public static string? GetMessageFormat(this ActivityLogEntity entity)
        {
            if (entity == null)
                return null;

            if (entity.MessageResourceKey == null)
            {
                // fall back to message if key is null
                return GetMessage(entity);
            }

            string? format = Resources.ResourceManager.GetString(entity.MessageResourceKey);
            return format ?? GetMessage(entity);
        }
    }
}
