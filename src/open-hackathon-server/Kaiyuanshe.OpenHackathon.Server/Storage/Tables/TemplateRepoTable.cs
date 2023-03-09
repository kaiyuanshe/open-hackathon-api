using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITemplateRepoTable : IAzureTableV2<TemplateRepoEntity>
    {
        Task<IEnumerable<TemplateRepoEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default);
    }

    public class TemplateRepoTable : AzureTableV2<TemplateRepoEntity>, ITemplateRepoTable
    {
        protected override string TableName => TableNames.TemplateRepo;

        #region ListByHackathonAsync
        public async Task<IEnumerable<TemplateRepoEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion
    }
}
