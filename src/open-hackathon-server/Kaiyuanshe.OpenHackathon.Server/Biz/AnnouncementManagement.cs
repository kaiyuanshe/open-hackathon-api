using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IAnnouncementManagement : IManagementClient, IDefaultManagementClient<Announcement, AnnouncementEntity, AnnouncementQueryOptions>
    {
        Task<AnnouncementEntity?> GetById(string hackathonName, string announcementId, CancellationToken cancellationToken);
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

        protected override void TryUpdate(AnnouncementEntity existing, Announcement parameter)
        {
            existing.Title = parameter.title ?? existing.Title;
            existing.Content = parameter.content ?? existing.Content;
        }

        protected override async Task<IEnumerable<AnnouncementEntity>> ListWithoutCache(AnnouncementQueryOptions options, CancellationToken cancellationToken)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(options.HackathonName);
            return await Table.QueryEntitiesAsync(filter, null, cancellationToken);
        }

        public async Task<AnnouncementEntity?> GetById(string hackathonName, string announcementId, CancellationToken cancellationToken)
        {
            return await Table.RetrieveAsync(hackathonName, announcementId, cancellationToken);
        }
    }
}
