using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IHackathonTable : IAzureTableV2<HackathonEntity>
    {
        Task<Dictionary<string, HackathonEntity>> ListAllHackathonsAsync(CancellationToken cancellationToken);
    }

    public class HackathonTable : AzureTableV2<HackathonEntity>, IHackathonTable
    {
        protected override string TableName => TableNames.Hackathon;

        public HackathonTable(ILogger<HackathonTable> logger) : base(logger)
        {

        }

        public async Task<Dictionary<string, HackathonEntity>> ListAllHackathonsAsync(CancellationToken cancellationToken)
        {
            var list = await QueryEntitiesAsync(null, null, cancellationToken);
            return list.ToDictionary(h => h.Name, h => h);
        }
    }
}
