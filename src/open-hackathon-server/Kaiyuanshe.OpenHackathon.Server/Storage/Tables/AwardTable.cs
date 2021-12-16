using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IAwardTable : IAzureTableV2<AwardEntity>
    {
        Task<IEnumerable<AwardEntity>> ListAllAwardsAsync(string hackathonName, CancellationToken cancellationToken = default);
    }

    public class AwardTable : AzureTableV2<AwardEntity>, IAwardTable
    {
        protected override string TableName => TableNames.Award;

        public AwardTable(ILogger<AwardTable> logger) : base(logger)
        {

        }

        #region ListAllAwardsAsync
        public async Task<IEnumerable<AwardEntity>> ListAllAwardsAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion
    }
}
