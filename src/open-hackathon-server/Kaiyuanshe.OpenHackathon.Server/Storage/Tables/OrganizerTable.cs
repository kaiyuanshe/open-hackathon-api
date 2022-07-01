using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IOrganizerTable : IAzureTableV2<OrganizerEntity>
    {
        Task<IEnumerable<OrganizerEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default);
    }

    public class OrganizerTable : AzureTableV2<OrganizerEntity>, IOrganizerTable
    {
        protected override string TableName => TableNames.Organizer;

        #region ListByHackathonAsync
        public async Task<IEnumerable<OrganizerEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion
    }
}
