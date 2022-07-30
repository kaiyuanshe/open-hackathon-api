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
    {
        /// <summary>
        /// Get a team member by userId
        /// </summary>
        Task<TeamMemberEntity?> GetById(string hackathonName, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the status of a member
        /// </summary>
        Task<TeamMemberEntity> UpdateTeamMemberStatusAsync(TeamMemberEntity member, TeamMemberStatus teamMemberStatus, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update the status of a member
        /// </summary>
        Task<TeamMemberEntity> UpdateTeamMemberRoleAsync(TeamMemberEntity member, TeamMemberRole teamMemberRole, CancellationToken cancellationToken = default);

    }

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
            return StorageContext.TeamMemberTable.ListByTeamAsync(options.HackathonName, options.TeamId, cancellationToken);
        }

        protected override void TryUpdate(TeamMemberEntity existing, TeamMember parameter)
        {
            existing.Description = parameter.description ?? existing.Description;
        }

        #region GetTeamMemberAsync
        public async Task<TeamMemberEntity?> GetById(string hackathonName, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(userId))
                return null;

            return await StorageContext.TeamMemberTable.RetrieveAsync(hackathonName, userId, cancellationToken);
        }
        #endregion

        #region UpdateTeamMemberStatusAsync
        public async Task<TeamMemberEntity> UpdateTeamMemberStatusAsync(TeamMemberEntity member, TeamMemberStatus teamMemberStatus, CancellationToken cancellationToken = default)
        {
            if (member.Status != teamMemberStatus)
            {
                member.Status = teamMemberStatus;
                await StorageContext.TeamMemberTable.MergeAsync(member);
            }

            return member;
        }
        #endregion

        #region UpdateTeamMemberRoleAsync
        public async Task<TeamMemberEntity> UpdateTeamMemberRoleAsync(TeamMemberEntity member, TeamMemberRole teamMemberRole, CancellationToken cancellationToken = default)
        {
            if (member.Role != teamMemberRole)
            {
                member.Role = teamMemberRole;
                await StorageContext.TeamMemberTable.MergeAsync(member);
            }

            return member;
        }
        #endregion
    }
}
