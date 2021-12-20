using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IJudgeTable : IAzureTableV2<JudgeEntity>
    {
        Task<IEnumerable<JudgeEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default);
    }

    public class JudgeTable : AzureTableV2<JudgeEntity>, IJudgeTable
    {
        protected override string TableName => TableNames.Judge;

        public JudgeTable(ILogger<JudgeTable> logger) : base(logger)
        {
        }

        #region Task<IEnumerable<JudgeEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default);
        public async Task<IEnumerable<JudgeEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion
    }
}
