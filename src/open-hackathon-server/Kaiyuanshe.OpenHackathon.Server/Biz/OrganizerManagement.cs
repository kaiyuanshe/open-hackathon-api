using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IOrganizerManagement
    {
        [return: NotNull]
        Task<OrganizerEntity> CreateOrganizer(string hackathonName, Organizer parameter, CancellationToken cancellationToken);
        Task<OrganizerEntity?> UpdateOrganizer(OrganizerEntity? entity, Organizer organizer, CancellationToken cancellationToken);
        Task<OrganizerEntity?> GetOrganizerById([DisallowNull] string hackathonName, [DisallowNull] string organizerId, CancellationToken cancellationToken);
    }

    public class OrganizerManagement : ManagementClientBase<OrganizerManagement>, IOrganizerManagement
    {
        #region CreateOrganizer
        [return: NotNull]
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
            return entity;
        }
        #endregion

        #region UpdateOrganizer
        public async Task<OrganizerEntity?> UpdateOrganizer(OrganizerEntity? entity, Organizer parameter, CancellationToken cancellationToken)
        {
            if (entity == null || parameter == null)
                return entity;

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
    }
}
