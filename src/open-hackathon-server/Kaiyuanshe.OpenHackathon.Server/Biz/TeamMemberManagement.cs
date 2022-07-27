using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface ITeamMemberManagement : IManagementClient, IDefaultManagementClient<TeamMember, TeamMemberEntity, TeamMemberQueryOptions>
    { }

    public class TeamMemberManagement : DefaultManagementClient<TeamMemberManagement, TeamMember, TeamMemberEntity, TeamMemberQueryOptions>, ITeamMemberManagement
    {
        protected override IAzureTableV2<TeamMemberEntity> Table => StorageContext.TeamMemberTable;

        protected override CacheEntryType CacheType => CacheEntryType.TeamMember;

        protected override TeamMemberEntity ConvertParamterToEntity(TeamMember parameter)
        {
            var entity = new TeamMemberEntity
            {
                PartitionKey = parameter.hackathonName,
                RowKey = parameter.userId,
                Description = parameter.description,
                TeamId = parameter.teamId,
                Role = parameter.role.GetValueOrDefault(TeamMemberRole.Member),
                Status = parameter.status,
                CreatedAt = DateTime.UtcNow,
            };

            return entity;
        }

        protected override Task<IEnumerable<TeamMemberEntity>> ListWithoutCache(TeamMemberQueryOptions options, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        protected override void TryUpdate(TeamMemberEntity existing, TeamMember parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
