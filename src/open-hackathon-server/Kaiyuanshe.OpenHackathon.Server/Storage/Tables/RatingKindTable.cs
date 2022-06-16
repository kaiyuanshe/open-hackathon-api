using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IRatingKindTable : IAzureTableV2<RatingKindEntity>
    {
        Task<IEnumerable<RatingKindEntity>> ListRatingKindsAsync(string hackathonName, CancellationToken cancellationToken = default);
    }

    public class RatingKindTable : AzureTableV2<RatingKindEntity>, IRatingKindTable
    {
        protected override string TableName => TableNames.RatingKind;

        #region Task<IEnumerable<RatingKindEntity>> ListRatingKindsAsync(string hackathonName, CancellationToken cancellationToken = default)
        public async Task<IEnumerable<RatingKindEntity>> ListRatingKindsAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion
    }
}
