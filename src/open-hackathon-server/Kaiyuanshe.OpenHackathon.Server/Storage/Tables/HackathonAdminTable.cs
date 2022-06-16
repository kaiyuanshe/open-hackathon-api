using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IHackathonAdminTable : IAzureTableV2<HackathonAdminEntity>
    {
        /// <summary>
        /// Get platform-wide role of current user
        /// </summary>
        /// <param name="userId">user Id</param>
        /// <param name="cancellationToken"></param>
        /// <returns><seealso cref="EnrollmentEntity"/> or null.</returns>
        Task<HackathonAdminEntity> GetPlatformRole(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// List all individual participants of a hackathon including admins, judges and contestents.
        /// </summary>
        /// <param name="hackathonName">name of hackathon. string.Empty for platform admin</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<HackathonAdminEntity>> ListByHackathonAsync(string hackathonName, CancellationToken cancellationToken);
    }

    public class HackathonAdminTable : AzureTableV2<HackathonAdminEntity>, IHackathonAdminTable
    {
        protected override string TableName => TableNames.HackathonAdmin;

        public async Task<HackathonAdminEntity> GetPlatformRole(string userId, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(string.Empty, userId, cancellationToken);
        }

        public async Task<IEnumerable<HackathonAdminEntity>> ListByHackathonAsync(string name, CancellationToken cancellationToken)
        {
            var filter = TableQueryHelper.PartitionKeyFilter(name);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
    }
}
