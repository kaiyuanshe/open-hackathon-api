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
    public interface IAnnouncementManagement
    {
        Task<AnnouncementEntity> CreateAnnouncement(string hackathonName, Announcement parameter, CancellationToken cancellationToken);
    }

    public class AnnouncementManagement : ManagementClientBase<AnnouncementManagement>, IAnnouncementManagement
    {
        #region CreateAnnouncement
        public async Task<AnnouncementEntity> CreateAnnouncement(string hackathonName, Announcement parameter, CancellationToken cancellationToken)
        {
            //var entity = new OrganizerEntity
            //{
            //    PartitionKey = hackathonName,
            //    RowKey = Guid.NewGuid().ToString(),
            //    CreatedAt = DateTime.UtcNow,
            //    Description = parameter.description,
            //    Logo = parameter.logo,
            //    Name = parameter.name,
            //    Type = parameter.type.GetValueOrDefault(OrganizerType.host),
            //};
            //await StorageContext.OrganizerTable.InsertAsync(entity, cancellationToken);
            // InvalidateCache(hackathonName);

            return null;
        }
        #endregion
    }
}
