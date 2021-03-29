﻿using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IHackathonManagement
    {
        #region Hackathon
        /// <summary>
        /// Create a new hackathon
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HackathonEntity> CreateHackathonAsync(Hackathon request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update hackathon from request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HackathonEntity> UpdateHackathonAsync(Hackathon request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Change the hackathon to Deleted.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteHackathonLogically(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get Hackathon By name. Return null if not found.
        /// </summary>
        /// <returns></returns>
        Task<HackathonEntity> GetHackathonEntityByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search hackathon
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<HackathonEntity>> SearchHackathonAsync(HackathonSearchOptions options, CancellationToken cancellationToken = default);

        #endregion

        #region Admin
        /// <summary>
        /// List all Administrators of a Hackathon. PlatformAdministrator is not included.
        /// </summary>
        /// <param name="name">name of Hackathon</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<HackathonAdminEntity>> ListHackathonAdminAsync(string name, CancellationToken cancellationToken = default);
        #endregion

        #region Enrollment
        /// <summary>
        /// Register a hackathon event as contestant
        /// </summary>
        Task<ParticipantEntity> EnrollAsync(HackathonEntity hackathon, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update status of enrollment.
        /// </summary>
        Task<ParticipantEntity> UpdateEnrollmentStatusAsync(ParticipantEntity participant, EnrollmentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get an enrollment.
        /// </summary>
        Task<ParticipantEntity> GetEnrollmentAsync(string hackathonName, string userId, CancellationToken cancellationToken = default);

        Task<ParticipantEntity> ListEnrollmentsAsync(string hackathonName, EnrollmentSearchOptions options, CancellationToken cancellationToken = default);
        #endregion
    }

    /// <inheritdoc cref="IHackathonManagement"/>
    public class HackathonManagement : ManagementClientBase, IHackathonManagement
    {
        private readonly ILogger Logger;

        public HackathonManagement(ILogger<HackathonManagement> logger)
        {
            Logger = logger;
        }

        #region Hackahton
        public async Task<HackathonEntity> CreateHackathonAsync(Hackathon request, CancellationToken cancellationToken = default)
        {
            #region Insert HackathonEntity
            var entity = new HackathonEntity
            {
                PartitionKey = request.name,
                RowKey = string.Empty,
                AutoApprove = request.autoApprove.HasValue ? request.autoApprove.Value : false,
                Ribbon = request.ribbon,
                Status = HackathonStatus.planning,
                Summary = request.summary,
                Tags = request.tags,
                MaxEnrollment = request.maxEnrollment.HasValue ? request.maxEnrollment.Value : 0,
                Banners = request.banners,
                CreatedAt = DateTime.UtcNow,
                CreatorId = request.creatorId,
                Detail = request.detail,
                DisplayName = request.displayName,
                EventStartedAt = request.eventStartedAt,
                EventEndedAt = request.eventEndedAt,
                EnrollmentStartedAt = request.enrollmentStartedAt,
                EnrollmentEndedAt = request.enrollmentEndedAt,
                IsDeleted = false,
                JudgeStartedAt = request.judgeStartedAt,
                JudgeEndedAt = request.judgeEndedAt,
                Location = request.location,
            };
            await StorageContext.HackathonTable.InsertAsync(entity, cancellationToken);
            #endregion

            #region Add creator as Admin
            ParticipantEntity participant = new ParticipantEntity
            {
                PartitionKey = request.name,
                RowKey = request.creatorId,
                CreatedAt = DateTime.UtcNow,
                Role = ParticipantRole.Administrator,
            };
            await StorageContext.ParticipantTable.InsertAsync(participant, cancellationToken);
            #endregion

            return entity;
        }

        public async Task DeleteHackathonLogically(string name, CancellationToken cancellationToken = default)
        {
            await StorageContext.HackathonTable.RetrieveAndMergeAsync(name, string.Empty,
                entity =>
                {
                    entity.IsDeleted = true;
                }, cancellationToken);
        }

        public async Task<HackathonEntity> GetHackathonEntityByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var entity = await StorageContext.HackathonTable.RetrieveAsync(name, string.Empty, cancellationToken);
            return entity;
        }

        public async Task<HackathonEntity> UpdateHackathonAsync(Hackathon request, CancellationToken cancellationToken = default)
        {
            await StorageContext.HackathonTable.RetrieveAndMergeAsync(request.name, string.Empty, (entity) =>
            {
                entity.Ribbon = request.ribbon ?? entity.Ribbon;
                entity.Summary = request.summary ?? entity.Summary;
                entity.Detail = request.detail ?? entity.Detail;
                entity.Location = request.location ?? entity.Location;
                entity.Banners = request.banners ?? entity.Banners;
                entity.DisplayName = request.displayName ?? entity.DisplayName;
                if (request.maxEnrollment.HasValue)
                    entity.MaxEnrollment = request.maxEnrollment.Value;
                if (request.autoApprove.HasValue)
                    entity.AutoApprove = request.autoApprove.Value;
                entity.Tags = request.tags ?? entity.Tags;
                if (request.eventStartedAt.HasValue)
                    entity.EventStartedAt = request.eventStartedAt.Value;
                if (request.eventEndedAt.HasValue)
                    entity.EventEndedAt = request.eventEndedAt.Value;
                if (request.enrollmentStartedAt.HasValue)
                    entity.EnrollmentStartedAt = request.enrollmentStartedAt.Value;
                if (request.enrollmentEndedAt.HasValue)
                    entity.EnrollmentEndedAt = request.enrollmentEndedAt.Value;
                if (request.judgeStartedAt.HasValue)
                    entity.JudgeStartedAt = request.judgeStartedAt.Value;
                if (request.judgeEndedAt.HasValue)
                    entity.JudgeEndedAt = request.judgeEndedAt.Value;
            }, cancellationToken);
            return await StorageContext.HackathonTable.RetrieveAsync(request.name, string.Empty, cancellationToken);
        }

        public async Task<IEnumerable<HackathonEntity>> SearchHackathonAsync(HackathonSearchOptions options, CancellationToken cancellationToken = default)
        {
            var entities = new List<HackathonEntity>();
            var filter = TableQuery.GenerateFilterConditionForBool(nameof(HackathonEntity.IsDeleted), QueryComparisons.NotEqual, true);
            TableQuery<HackathonEntity> query = new TableQuery<HackathonEntity>().Where(filter);

            await StorageContext.HackathonTable.ExecuteQuerySegmentedAsync(query, (segment) =>
            {
                entities.AddRange(segment);
            }, cancellationToken);

            return entities;
        }
        #endregion

        #region Enrollment
        public async Task<ParticipantEntity> GetEnrollmentAsync(string hackathonName, string userId, CancellationToken cancellationToken = default)
        {
            if (hackathonName == null || userId == null)
                return null;
            return await StorageContext.ParticipantTable.RetrieveAsync(hackathonName.ToLower(), userId.ToLower(), cancellationToken);
        }

        public async Task<ParticipantEntity> EnrollAsync(HackathonEntity hackathon, string userId, CancellationToken cancellationToken)
        {
            string hackathonName = hackathon.Name;
            var entity = await StorageContext.ParticipantTable.RetrieveAsync(hackathonName, userId, cancellationToken);

            if (entity != null && entity.Role.HasFlag(ParticipantRole.Contestant))
            {
                Logger.TraceInformation($"Enroll skipped, user with id {userId} alreday enrolled in hackathon {hackathonName}");
                return entity;
            }

            if (entity != null)
            {
                entity.Role = entity.Role | ParticipantRole.Contestant;
                entity.Status = EnrollmentStatus.pending;
                if (hackathon.AutoApprove)
                {
                    entity.Status = EnrollmentStatus.approved;
                }
                await StorageContext.ParticipantTable.MergeAsync(entity, cancellationToken);
            }
            else
            {
                entity = new ParticipantEntity
                {
                    PartitionKey = hackathonName,
                    RowKey = userId,
                    Role = ParticipantRole.Contestant,
                    Status = EnrollmentStatus.pending,
                    CreatedAt = DateTime.UtcNow,
                };
                if (hackathon.AutoApprove)
                {
                    entity.Status = EnrollmentStatus.approved;
                }
                await StorageContext.ParticipantTable.InsertAsync(entity, cancellationToken);
            }
            Logger.TraceInformation($"user {userId} enrolled in hackathon {hackathon}, status: {entity.Status.ToString()}");

            return entity;
        }

        public async Task<ParticipantEntity> UpdateEnrollmentStatusAsync(ParticipantEntity participant, EnrollmentStatus status, CancellationToken cancellationToken = default)
        {
            if (participant == null)
                return participant;

            participant.Status = status;
            await StorageContext.ParticipantTable.MergeAsync(participant, cancellationToken);
            Logger.TraceInformation($"Pariticipant {participant.HackathonName}/{participant.UserId} stastus updated to: {status} ");
            return participant;
        }

        public async Task<ParticipantEntity> ListEnrollmentsAsync(string hackathonName, EnrollmentSearchOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Admin

        public async Task<IEnumerable<HackathonAdminEntity>> ListHackathonAdminAsync(string name, CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheKey.Get(CacheKey.Section.HackathonAdmin, name);
            return await CacheHelper.GetOrAddAsync(cacheKey,
                async () =>
                {
                    return await StorageContext.HackathonAdminTable.ListByHackathonAsync(name, cancellationToken);
                },
                CacheHelper.ExpireIn10M);
        }
        #endregion
    }

    public class HackathonSearchOptions
    {

    }

    public class EnrollmentSearchOptions
    {
        public TableContinuationToken TableContinuationToken { get; set; }
        public EnrollmentStatus? Status { get; set; }
    }
}
