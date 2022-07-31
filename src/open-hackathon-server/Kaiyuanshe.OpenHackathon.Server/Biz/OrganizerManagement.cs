using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IOrganizerManagement
    {
        Task<OrganizerEntity> CreateOrganizer(string hackathonName, Organizer parameter, CancellationToken cancellationToken);
        Task<OrganizerEntity> UpdateOrganizer(OrganizerEntity entity, Organizer organizer, CancellationToken cancellationToken);
        Task<OrganizerEntity?> GetOrganizerById([DisallowNull] string hackathonName, [DisallowNull] string organizerId, CancellationToken cancellationToken);
        Task<IEnumerable<OrganizerEntity>> ListPaginatedOrganizersAsync(string hackathonName, OrganizerQueryOptions options, CancellationToken cancellationToken = default);
        Task DeleteOrganizer(string hackathonName, string organizerId, CancellationToken cancellationToken);
    }

    public class OrganizerManagement : ManagementClient<OrganizerManagement>, IOrganizerManagement
    {
        #region Cache
        private string CacheKeyByHackathon(string hackathonName)
        {
            return CacheKeys.GetCacheKey(CacheEntryType.Organizer, hackathonName);
        }

        private void InvalidateCachedOrganizers(string hackathonName)
        {
            Cache.Remove(CacheKeyByHackathon(hackathonName));
        }

        private async Task<IEnumerable<OrganizerEntity>> GetCachedOrganizers(string hackathonName, CancellationToken cancellationToken)
        {
            string cacheKey = CacheKeyByHackathon(hackathonName);
            return await Cache.GetOrAddAsync(cacheKey, TimeSpan.FromHours(6), (ct) =>
            {
                return StorageContext.OrganizerTable.ListByHackathonAsync(hackathonName, ct);
            }, true, cancellationToken);
        }
        #endregion

        #region CreateOrganizer
        public async Task<OrganizerEntity> CreateOrganizer(string hackathonName, Organizer parameter, CancellationToken cancellationToken)
        {
            var entity = new OrganizerEntity
            {
                PartitionKey = hackathonName,
                RowKey = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Description = parameter.description,
                Logo = parameter.logo,
                Name = parameter.name,
                Type = parameter.type.GetValueOrDefault(OrganizerType.host),
            };
            await StorageContext.OrganizerTable.InsertAsync(entity, cancellationToken);
            InvalidateCachedOrganizers(hackathonName);

            return entity;
        }
        #endregion

        #region UpdateOrganizer
        public async Task<OrganizerEntity> UpdateOrganizer(OrganizerEntity entity, Organizer parameter, CancellationToken cancellationToken)
        {
            entity.Name = parameter.name ?? entity.Name;
            entity.Description = parameter.description ?? entity.Description;
            entity.Type = parameter.type.GetValueOrDefault(entity.Type);
            if (parameter.logo != null)
            {
                if (entity.Logo == null)
                    entity.Logo = parameter.logo;
                else
                {
                    entity.Logo.name = parameter.logo.name ?? entity.Logo.name;
                    entity.Logo.description = parameter.logo.description ?? entity.Logo.description;
                    entity.Logo.uri = parameter.logo.uri ?? entity.Logo.uri;
                }
            }
            await StorageContext.OrganizerTable.MergeAsync(entity, cancellationToken);
            InvalidateCachedOrganizers(entity.HackathonName);
            return entity;
        }
        #endregion

        #region GetOrganizerById
        public async Task<OrganizerEntity?> GetOrganizerById(
            [DisallowNull] string hackathonName,
            [DisallowNull] string organizerId,
            CancellationToken cancellationToken)
        {
            return await StorageContext.OrganizerTable.RetrieveAsync(hackathonName, organizerId, cancellationToken);
        }
        #endregion

        #region ListPaginatedOrganizersAsync
        public async Task<IEnumerable<OrganizerEntity>> ListPaginatedOrganizersAsync(string hackathonName, OrganizerQueryOptions options, CancellationToken cancellationToken = default)
        {
            var allOrganizers = await GetCachedOrganizers(hackathonName, cancellationToken);

            // paging
            int.TryParse(options.Pagination?.np, out int np);
            int top = options.Top();
            var organizers = allOrganizers.OrderByDescending(a => a.CreatedAt).Skip(np).Take(top);

            // next paging
            options.NextPage = null;
            if (np + top < allOrganizers.Count())
            {
                options.NextPage = new Pagination
                {
                    np = (np + top).ToString(),
                    nr = (np + top).ToString(),
                };
            }

            return organizers;
        }
        #endregion

        #region DeleteOrganizer
        public async Task DeleteOrganizer(string hackathonName, string organizerId, CancellationToken cancellationToken)
        {
            await StorageContext.OrganizerTable.DeleteAsync(hackathonName, organizerId, cancellationToken);
            InvalidateCachedOrganizers(hackathonName);
        }
        #endregion
    }
}
