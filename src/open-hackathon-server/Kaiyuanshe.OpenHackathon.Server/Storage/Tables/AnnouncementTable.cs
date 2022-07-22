using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IAnnouncementTable : IAzureTableV2<AnnouncementEntity>
    {
        Task<IEnumerable<AnnouncementEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default);
    }

    public class AnnouncementTable : AzureTableV2<AnnouncementEntity>, IAnnouncementTable
    {
        protected override string TableName => TableNames.Announcement;

        public async Task<IEnumerable<AnnouncementEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
    }
}
