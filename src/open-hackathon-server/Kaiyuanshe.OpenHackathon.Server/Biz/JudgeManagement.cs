﻿using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IJudgeManagement
    {
        Task<bool> CanCreateJudgeAsync(string hackathonName, CancellationToken cancellationToken);
        Task<JudgeEntity> CreateJudgeAsync(Judge parameter, CancellationToken cancellationToken);
        Task<JudgeEntity> UpdateJudgeAsync(JudgeEntity exising, Judge parameter, CancellationToken cancellationToken);
        Task<JudgeEntity?> GetJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken);
        Task<bool> IsJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<JudgeEntity>> ListPaginatedJudgesAsync(string hackathonName, JudgeQueryOptions options, CancellationToken cancellationToken = default);
        Task DeleteJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken);
    }

    public class JudgeManagement : ManagementClient<JudgeManagement>, IJudgeManagement
    {
        static readonly int MaxJudgeCount = 100;

        #region Cache
        private string CacheKeyByHackathon(string hackathonName)
        {
            return CacheKeys.GetCacheKey(CacheEntryType.Judge, hackathonName);
        }

        private void InvalidateCachedJudges(string hackathonName)
        {
            Cache.Remove(CacheKeyByHackathon(hackathonName));
        }

        private async Task<IEnumerable<JudgeEntity>> GetCachedJudges(string hackathonName, CancellationToken cancellationToken)
        {
            string cacheKey = CacheKeyByHackathon(hackathonName);
            return await Cache.GetOrAddAsync(cacheKey, TimeSpan.FromHours(6), (ct) =>
            {
                return StorageContext.JudgeTable.ListByHackathonAsync(hackathonName, ct);
            }, false, cancellationToken);
        }
        #endregion

        #region Task<bool> CanCreateJudgeAsync(string hackathonName, CancellationToken cancellationToken)
        public async Task<bool> CanCreateJudgeAsync(string hackathonName, CancellationToken cancellationToken)
        {
            var judges = await GetCachedJudges(hackathonName, cancellationToken);
            return judges.Count() < MaxJudgeCount;
        }
        #endregion

        #region Task<JudgeEntity> CreateJudgeAsync(Judge parameter, CancellationToken cancellationToken);
        public async Task<JudgeEntity> CreateJudgeAsync(Judge parameter, CancellationToken cancellationToken)
        {
            JudgeEntity entity = new JudgeEntity
            {
                PartitionKey = parameter.hackathonName,
                RowKey = parameter.userId,
                CreatedAt = DateTime.UtcNow,
                Description = parameter.description,
            };
            await StorageContext.JudgeTable.InsertOrReplaceAsync(entity, cancellationToken);
            InvalidateCachedJudges(parameter.hackathonName);
            return entity;
        }
        #endregion

        #region UpdateJudgeAsync
        public async Task<JudgeEntity> UpdateJudgeAsync(JudgeEntity exising, Judge parameter, CancellationToken cancellationToken)
        {
            exising.Description = parameter.description ?? exising.Description;
            await StorageContext.JudgeTable.MergeAsync(exising, cancellationToken);
            InvalidateCachedJudges(parameter.hackathonName);
            return exising;
        }
        #endregion

        #region Task<JudgeEntity> GetJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken);
        public async Task<JudgeEntity?> GetJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(userId))
                return null;

            return await StorageContext.JudgeTable.RetrieveAsync(hackathonName, userId, cancellationToken);
        }
        #endregion

        #region Task<bool> IsJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken = default);
        public async Task<bool> IsJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(userId))
                return false;

            var judges = await GetCachedJudges(hackathonName, cancellationToken);
            return judges.Any(j => j.UserId == userId);
        }
        #endregion

        #region Task<IEnumerable<JudgeEntity>> ListPaginatedJudgesAsync(string hackathonName, JudgeQueryOptions options, CancellationToken cancellationToken = default);
        public async Task<IEnumerable<JudgeEntity>> ListPaginatedJudgesAsync(string hackathonName, JudgeQueryOptions options, CancellationToken cancellationToken = default)
        {
            IEnumerable<JudgeEntity> allJudges = await GetCachedJudges(hackathonName, cancellationToken);

            // paging
            int.TryParse(options.Pagination?.np, out int np);
            int top = options.Top();
            var judges = allJudges.OrderByDescending(a => a.CreatedAt).Skip(np).Take(top);

            // next paging
            options.NextPage = null;
            if (np + top < allJudges.Count())
            {
                options.NextPage = new Pagination
                {
                    np = (np + top).ToString(),
                    nr = (np + top).ToString(),
                };
            }

            return judges;
        }
        #endregion

        #region DeleteJudgeAsync
        public async Task DeleteJudgeAsync(string hackathonName, string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(userId))
                return;

            await StorageContext.JudgeTable.DeleteAsync(hackathonName, userId, cancellationToken);
            InvalidateCachedJudges(hackathonName);
        }
        #endregion
    }
}
