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
        Task<OrganizerEntity> CreateOrganizer(string hackathonName, Organizer organizer, CancellationToken cancellationToken);
    }

    public class OrganizerManagement : ManagementClientBase<OrganizerManagement>, IOrganizerManagement
    {
        #region CreateOrganizer
        [return: NotNull]
        public async Task<OrganizerEntity> CreateOrganizer(string hackathonName, Organizer organizer, CancellationToken cancellationToken)
        {
            var entity = new OrganizerEntity
            {
                PartitionKey = hackathonName,
                RowKey = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Description = organizer.description,
                Logo = organizer.logo,
                Name = organizer.name,
                Type = organizer.type.GetValueOrDefault(OrganizerType.host),
            };
            await StorageContext.OrganizerTable.InsertAsync(entity, cancellationToken);
            return entity;
        }
        #endregion
    }
}
