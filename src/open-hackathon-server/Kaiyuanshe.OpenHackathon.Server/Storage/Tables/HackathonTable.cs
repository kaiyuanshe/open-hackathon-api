using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IHackathonTable : IAzureTableV2<HackathonEntity>
    {
        /// <summary>
        /// List all hackathons except offline ones.
        /// </summary>
        Task<Dictionary<string, HackathonEntity>> ListAllHackathonsAsync(CancellationToken cancellationToken);
    }

    public class HackathonTable : AzureTableV2<HackathonEntity>, IHackathonTable
    {
        protected override string TableName => TableNames.Hackathon;

        public async Task<Dictionary<string, HackathonEntity>> ListAllHackathonsAsync(CancellationToken cancellationToken)
        {
            string query = TableQueryHelper.FilterForInt(nameof(HackathonEntity.Status), ComparisonOperator.NotEqual, (int)HackathonStatus.offline);
            var list = await QueryEntitiesAsync(query, null, cancellationToken);
            return list.ToDictionary(h => h.Name, h => h);
        }
    }
}
