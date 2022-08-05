using Azure;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IEnrollmentManagement
    {
        /// <summary>
        /// Register a hackathon event as contestant
        /// </summary>
        Task<EnrollmentEntity> CreateEnrollmentAsync(HackathonEntity hackathon, Enrollment request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an enrollment
        /// </summary>
        Task<EnrollmentEntity> UpdateEnrollmentAsync(EnrollmentEntity existing, Enrollment request, CancellationToken cancellationToken);

        /// <summary>
        /// Update status of enrollment.
        /// </summary>
        Task<EnrollmentEntity> UpdateEnrollmentStatusAsync(HackathonEntity hackathon, EnrollmentEntity participant, EnrollmentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// List paged enrollments of hackathon
        /// </summary>
        /// <param name="hackathonName">name of hackathon</param>
        /// <param name="options">options for query</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Page<EnrollmentEntity>> ListPaginatedEnrollmentsAsync(string hackathonName, EnrollmentQueryOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get an enrollment.
        /// </summary>
        Task<EnrollmentEntity?> GetEnrollmentAsync(string hackathonName, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check whether a user enrolled in a hackathon. It's similar to GetEnrollmentAsync. 
        /// But if we are checking a lot of users/hackathons from a loop, use IsUserEnrolledAsync which trying best to read cached data.
        /// </summary>
        /// <param name="hackathon"></param>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsUserEnrolledAsync(HackathonEntity hackathon, string userId, CancellationToken cancellationToken = default);
    }

    public class EnrollmentManagement : ManagementClient<EnrollmentManagement>, IEnrollmentManagement
    {
        // only cache enrollments when its count is <=1000, to avoid too many memory being used.
        private static readonly int CACHE_THRESHOLD = 1000;

        #region Cache
        private void InvalidateCachedEnrollment(string hackathonName)
        {
            string cacheKey = CacheKeys.GetCacheKey(CacheEntryType.Enrollment, hackathonName);
            Cache.Remove(cacheKey);
        }

        private async Task<IEnumerable<EnrollmentEntity>?> GetCachedEnrollmentsAsync(HackathonEntity hackathon, CancellationToken cancellationToken = default)
        {
            if (hackathon == null || hackathon.MaxEnrollment > CACHE_THRESHOLD || hackathon.MaxEnrollment <= 0)
            {
                return null;
            }

            return await Cache.GetOrAddAsync(
                CacheKeys.GetCacheKey(CacheEntryType.Enrollment, hackathon.Name),
                TimeSpan.FromHours(4),
                async (ct) =>
                {
                    var filter = TableQueryHelper.PartitionKeyFilter(hackathon.Name);
                    return await StorageContext.EnrollmentTable.QueryEntitiesAsync(filter, null, cancellationToken);
                },
                false,
                cancellationToken);
        }
        #endregion

        #region CreateEnrollmentAsync
        public async Task<EnrollmentEntity> CreateEnrollmentAsync(HackathonEntity hackathon, Enrollment request, CancellationToken cancellationToken)
        {
            string hackathonName = hackathon.Name;
            var entity = new EnrollmentEntity
            {
                PartitionKey = hackathonName,
                RowKey = request.userId,
                Status = EnrollmentStatus.pendingApproval,
                CreatedAt = DateTime.UtcNow,
                Extensions = request.extensions.Merge(null),
            };
            if (hackathon.AutoApprove)
            {
                entity.Status = EnrollmentStatus.approved;
            }
            await StorageContext.EnrollmentTable.InsertAsync(entity, cancellationToken);
            InvalidateCachedEnrollment(hackathonName);
            Logger?.TraceInformation($"user {request.userId} enrolled in hackathon {hackathon}, status: {entity.Status.ToString()}");

            if (entity.Status == EnrollmentStatus.approved)
            {
                hackathon.Enrollment += 1;
                await StorageContext.HackathonTable.MergeAsync(hackathon, cancellationToken);
            }

            return entity;
        }
        #endregion

        #region UpdateEnrollmentAsync
        public async Task<EnrollmentEntity> UpdateEnrollmentAsync(EnrollmentEntity existing, Enrollment request, CancellationToken cancellationToken = default)
        {
            existing.Extensions = existing.Extensions.Merge(request.extensions);
            await StorageContext.EnrollmentTable.MergeAsync(existing, cancellationToken);
            InvalidateCachedEnrollment(existing.HackathonName);

            return existing;
        }
        #endregion

        #region UpdateEnrollmentStatusAsync
        public async Task<EnrollmentEntity> UpdateEnrollmentStatusAsync(HackathonEntity hackathon, EnrollmentEntity enrollment, EnrollmentStatus status, CancellationToken cancellationToken = default)
        {
            if (enrollment.Status == status)
                return enrollment;

            // update hackathon.enrollment
            int enrollmentIncreasement = 0;
            if (status == EnrollmentStatus.approved)
            {
                enrollmentIncreasement += 1;
            }
            if (enrollment.Status == EnrollmentStatus.approved && hackathon.Enrollment >= 1)
            {
                enrollmentIncreasement -= 1;
            }

            enrollment.Status = status;
            await StorageContext.EnrollmentTable.MergeAsync(enrollment, cancellationToken);
            InvalidateCachedEnrollment(hackathon.Name);

            if (enrollmentIncreasement != 0)
            {
                hackathon.Enrollment += enrollmentIncreasement;
                await StorageContext.HackathonTable.MergeAsync(hackathon, cancellationToken);
            }
            Logger?.TraceInformation($"Pariticipant {enrollment.HackathonName}/{enrollment.UserId} stastus updated to: {status} ");

            return enrollment;
        }
        #endregion

        #region ListPaginatedEnrollmentsAsync
        public async Task<Page<EnrollmentEntity>> ListPaginatedEnrollmentsAsync(string hackathonName, EnrollmentQueryOptions options, CancellationToken cancellationToken = default)
        {
            Debug.Assert(options != null);
            var filter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            if (options != null && options.Status.HasValue)
            {
                var statusFilter = TableQueryHelper.FilterForInt(
                    nameof(EnrollmentEntity.Status),
                     ComparisonOperator.Equal,
                    (int)options.Status.Value);
                filter = TableQueryHelper.And(filter, statusFilter);
            }

            var page = await StorageContext.EnrollmentTable.ExecuteQuerySegmentedAsync(filter, options.ContinuationToken(), options.Top(), null, cancellationToken);
            return page;
        }
        #endregion

        #region GetEnrollmentAsync
        public virtual async Task<EnrollmentEntity?> GetEnrollmentAsync(string hackathonName, string userId, CancellationToken cancellationToken = default)
        {
            if (hackathonName == null || userId == null)
                return null;

            return await StorageContext.EnrollmentTable.RetrieveAsync(hackathonName.ToLower(), userId, cancellationToken);
        }
        #endregion

        #region IsUserEnrolledAsync
        public async Task<bool> IsUserEnrolledAsync(HackathonEntity hackathon, string userId, CancellationToken cancellationToken = default)
        {
            if (hackathon == null || string.IsNullOrWhiteSpace(userId))
                return false;

            EnrollmentEntity? entity = null;
            var cached = await GetCachedEnrollmentsAsync(hackathon, cancellationToken);
            if (cached == null)
            {
                entity = await StorageContext.EnrollmentTable.RetrieveAsync(hackathon.Name, userId, cancellationToken);
            }
            else
            {
                entity = cached.SingleOrDefault(e => e.UserId == userId);
            }

            if (entity == null)
                return false;
            return entity.Status == EnrollmentStatus.approved;
        }
        #endregion
    }
}
