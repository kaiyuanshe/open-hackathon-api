using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IAnnouncementManagement : IManagementClient
    {
        Task<AnnouncementEntity> CreateAnnouncement(Announcement parameter, CancellationToken cancellationToken);
        Task<AnnouncementEntity> UpdateAnnouncement(AnnouncementEntity entity, Announcement organizer, CancellationToken cancellationToken);
        Task<AnnouncementEntity?> GetById(string hackathonName, string announcementId, CancellationToken cancellationToken);
        Task<IEnumerable<AnnouncementEntity>> ListPaginatedAnnouncementsAsync(AnnouncementQueryOptions options, CancellationToken cancellationToken = default);
        Task DeleteAnnouncement(AnnouncementEntity announcementEntity, CancellationToken cancellationToken);

    }

    public class AnnouncementManagement : ManagementClient<AnnouncementManagement>, IAnnouncementManagement
    {

        #region Cache
        private string GetCacheKey(string hackathonName)
        {
            return CacheKeys.GetCacheKey(CacheEntryType.Announcement, hackathonName);
        }

        private void InvalidateCache(string hackathonName)
        {
            Cache.Remove(GetCacheKey(hackathonName));
        }

        private async Task<IEnumerable<AnnouncementEntity>> ListCachedEntities(AnnouncementQueryOptions options, CancellationToken cancellationToken)
        {
            string cacheKey = GetCacheKey(options.HackathonName);
            return await Cache.GetOrAddAsync(cacheKey, TimeSpan.FromHours(6), async (ct) =>
            {
                return await StorageContext.AnnouncementTable.ListByHackathonAsync(options.HackathonName, ct);
            }, false, cancellationToken);
        }
        #endregion

        public async Task<AnnouncementEntity> CreateAnnouncement(Announcement parameter, CancellationToken cancellationToken)
        {
            var entity = new AnnouncementEntity
            {
                PartitionKey = parameter.hackathonName,
                RowKey = Guid.NewGuid().ToString(),
                Title = parameter.title,
                Content = parameter.content,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow,
            };
            await StorageContext.AnnouncementTable.InsertAsync(entity, cancellationToken);
            InvalidateCache(entity.HackathonName);
            return entity;
        }

        public async Task<AnnouncementEntity> UpdateAnnouncement(AnnouncementEntity existing, Announcement parameter, CancellationToken cancellationToken)
        {
            existing.Title = parameter.title ?? existing.Title;
            existing.Content = parameter.content ?? existing.Content;
            await StorageContext.AnnouncementTable.MergeAsync(existing, cancellationToken);
            InvalidateCache(existing.HackathonName);
            return existing;
        }

        public async Task<AnnouncementEntity?> GetById(string hackathonName, string announcementId, CancellationToken cancellationToken)
        {
            return await StorageContext.AnnouncementTable.RetrieveAsync(hackathonName, announcementId, cancellationToken);
        }

        public async Task<IEnumerable<AnnouncementEntity>> ListPaginatedAnnouncementsAsync(AnnouncementQueryOptions options, CancellationToken cancellationToken = default)
        {
            var allEntities = await ListCachedEntities(options, cancellationToken);

            // paging
            int.TryParse(options.Pagination?.np, out int np);
            int top = options.Top();
            var filtered = allEntities.OrderByDescending(a => a.CreatedAt).Skip(np).Take(top);

            // next paging
            options.NextPage = null;
            if (np + top < allEntities.Count())
            {
                options.NextPage = new Pagination
                {
                    np = (np + top).ToString(),
                    nr = (np + top).ToString(),
                };
            }

            return filtered;
        }

        public async Task DeleteAnnouncement(AnnouncementEntity entity, CancellationToken cancellationToken)
        {
            await StorageContext.AnnouncementTable.DeleteAsync(entity.PartitionKey, entity.RowKey, cancellationToken);
        }
    }
}
