using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using System;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IAnnouncementManagement : IManagementClient, IDefaultManagementClient<Announcement, AnnouncementEntity, AnnouncementQueryOptions>
    {
    }

    public class AnnouncementManagement
        : DefaultManagementClient<AnnouncementManagement, Announcement, AnnouncementEntity, AnnouncementQueryOptions>, IAnnouncementManagement
    {
        protected override CacheEntryType CacheType => CacheEntryType.Announcement;
        protected override IAzureTableV2<AnnouncementEntity> Table => StorageContext.AnnouncementTable;
        protected override AnnouncementEntity ConvertParamterToEntity(Announcement parameter)
        {
            return new AnnouncementEntity
            {
                PartitionKey = parameter.hackathonName,
                RowKey = Guid.NewGuid().ToString(),
                Title = parameter.title,
                Content = parameter.content,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
