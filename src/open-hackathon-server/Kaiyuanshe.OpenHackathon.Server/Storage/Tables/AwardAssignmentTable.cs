using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IAwardAssignmentTable : IAzureTableV2<AwardAssignmentEntity>
    {
        Task<IEnumerable<AwardAssignmentEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default);

        Task<IEnumerable<AwardAssignmentEntity>> ListByAwardAsync(string hackathonName, string awardId, CancellationToken cancellationToken = default);

        Task<IEnumerable<AwardAssignmentEntity>> ListByAssigneeAsync(string hackathonName, string assigneeId, CancellationToken cancellationToken = default);

    }

    public class AwardAssignmentTable : AzureTableV2<AwardAssignmentEntity>, IAwardAssignmentTable
    {
        override protected string TableName => TableNames.AwardAssignment;

        #region ListByHackathonAsync
        public async Task<IEnumerable<AwardAssignmentEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion

        #region ListByAwardAsync
        public async Task<IEnumerable<AwardAssignmentEntity>> ListByAwardAsync(string hackathonName, string awardId, CancellationToken cancellationToken = default)
        {
            var hackathonNameFilter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            var awardIdFilter = TableQueryHelper.FilterForString(
                nameof(AwardAssignmentEntity.AwardId), 
                ComparisonOperator.Equal,
                awardId);
            var filter = TableQueryHelper.And(hackathonNameFilter, awardIdFilter);

            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion

        #region ListByAssigneeAsync
        public async Task<IEnumerable<AwardAssignmentEntity>> ListByAssigneeAsync(string hackathonName, string assigneeId, CancellationToken cancellationToken = default)
        {
            var hackathonNameFilter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            var assigneeIdFilter = TableQueryHelper.FilterForString(
                nameof(AwardAssignmentEntity.AssigneeId),
                ComparisonOperator.Equal,
                assigneeId);
            var filter = TableQueryHelper.And(hackathonNameFilter, assigneeIdFilter);

            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
        #endregion
    }
}
