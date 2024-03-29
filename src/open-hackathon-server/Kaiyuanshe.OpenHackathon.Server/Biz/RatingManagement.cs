﻿using Azure;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IRatingManagement
    {
        Task<bool> CanCreateRatingKindAsync(string hackathonName, CancellationToken cancellationToken);
        Task<RatingKindEntity> CreateRatingKindAsync(RatingKind parameter, CancellationToken cancellationToken);
        Task<RatingKindEntity> UpdateRatingKindAsync(RatingKindEntity existing, RatingKind parameter, CancellationToken cancellationToken);
        Task<RatingKindEntity?> GetCachedRatingKindAsync(string hackathonName, string kindId, CancellationToken cancellationToken);
        Task<RatingKindEntity?> GetRatingKindAsync(string hackathonName, string kindId, CancellationToken cancellationToken);
        Task<IEnumerable<RatingKindEntity>> ListPaginatedRatingKindsAsync(string hackathonName, RatingKindQueryOptions options, CancellationToken cancellationToken = default);
        Task DeleteRatingKindAsync(string hackathonName, string kindId, CancellationToken cancellationToken);
        Task<RatingEntity> CreateRatingAsync(Rating parameter, CancellationToken cancellationToken);
        Task<RatingEntity> UpdateRatingAsync(RatingEntity existing, Rating parameter, CancellationToken cancellationToken);
        Task<RatingEntity?> GetRatingAsync(string hackathonName, string judgeId, string teamId, string kindId, CancellationToken cancellationToken);
        Task<RatingEntity?> GetRatingAsync(string hackathonName, string ratingId, CancellationToken cancellationToken);
        Task<bool> IsRatingCountGreaterThanZero(string hackathonName, RatingQueryOptions options, CancellationToken cancellationToken);
        Task<Page<RatingEntity>> ListPaginatedRatingsAsync(string hackathonName, RatingQueryOptions options, CancellationToken cancellationToken = default);
        Task DeleteRatingAsync(string hackathonName, string ratingId, CancellationToken cancellationToken);
    }

    public class RatingManagement : ManagementClient<RatingManagement>, IRatingManagement
    {
        static readonly int MaxRatingKindCount = 100;

        #region Cache
        private string CacheKeyRatingKinds(string hackathonName)
        {
            return CacheKeys.GetCacheKey(CacheEntryType.RatingKind, hackathonName);
        }

        private void InvalidateCachedRatingKinds(string hackathonName)
        {
            Cache.Remove(CacheKeyRatingKinds(hackathonName));
        }

        #endregion

        #region CanCreateRatingKindAsync
        public async Task<bool> CanCreateRatingKindAsync(string hackathonName, CancellationToken cancellationToken)
        {
            var kinds = await ListRatingKindsAsync(hackathonName, cancellationToken);
            return kinds.Count() < MaxRatingKindCount;
        }
        #endregion

        #region CreateRatingKindAsync
        public async Task<RatingKindEntity> CreateRatingKindAsync(RatingKind parameter, CancellationToken cancellationToken)
        {
            var entity = new RatingKindEntity
            {
                PartitionKey = parameter.hackathonName,
                RowKey = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                Description = parameter.description,
                MaximumScore = parameter.maximumScore.GetValueOrDefault(10),
                Name = parameter.name,
            };
            await StorageContext.RatingKindTable.InsertAsync(entity, cancellationToken);
            InvalidateCachedRatingKinds(parameter.hackathonName);
            return entity;
        }
        #endregion

        #region UpdateRatingKindAsync
        public async Task<RatingKindEntity> UpdateRatingKindAsync(RatingKindEntity existing, RatingKind parameter, CancellationToken cancellationToken)
        {
            existing.Name = parameter.name ?? existing.Name;
            existing.Description = parameter.description ?? existing.Description;
            existing.MaximumScore = parameter.maximumScore.GetValueOrDefault(existing.MaximumScore);
            await StorageContext.RatingKindTable.MergeAsync(existing, cancellationToken);
            InvalidateCachedRatingKinds(existing.HackathonName);

            return existing;
        }
        #endregion

        #region GetCachedRatingKindAsync
        public async Task<RatingKindEntity?> GetCachedRatingKindAsync(string hackathonName, string kindId, CancellationToken cancellationToken)
        {
            var allKinds = await ListRatingKindsAsync(hackathonName, cancellationToken);
            return allKinds.FirstOrDefault(k => k.Id == kindId);
        }
        #endregion

        #region GetRatingKindAsync
        public async Task<RatingKindEntity?> GetRatingKindAsync(string hackathonName, string kindId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(kindId))
                return null;

            return await StorageContext.RatingKindTable.RetrieveAsync(hackathonName, kindId, cancellationToken);
        }
        #endregion

        #region ListPaginatedRatingKindsAsync
        private async Task<IEnumerable<RatingKindEntity>> ListRatingKindsAsync(string hackathonName, CancellationToken cancellationToken = default)
        {
            string cacheKey = CacheKeyRatingKinds(hackathonName);
            return await Cache.GetOrAddAsync(
                cacheKey,
                TimeSpan.FromHours(4),
                (ct) => StorageContext.RatingKindTable.ListRatingKindsAsync(hackathonName, ct),
                false,
                cancellationToken);
        }

        public async Task<IEnumerable<RatingKindEntity>> ListPaginatedRatingKindsAsync(string hackathonName, RatingKindQueryOptions options, CancellationToken cancellationToken = default)
        {
            var allKinds = await ListRatingKindsAsync(hackathonName, cancellationToken);

            // paging
            int.TryParse(options.Pagination?.np, out int np);
            int top = options.Top();
            var kinds = allKinds.OrderByDescending(a => a.CreatedAt).Skip(np).Take(top);

            // next paging
            options.NextPage = null;
            if (np + top < allKinds.Count())
            {
                options.NextPage = new Pagination
                {
                    np = (np + top).ToString(),
                    nr = (np + top).ToString(),
                };
            }

            return kinds;
        }
        #endregion

        #region DeleteJudgeAsync
        public async Task DeleteRatingKindAsync(string hackathonName, string kindId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(kindId))
                return;

            await StorageContext.RatingKindTable.DeleteAsync(hackathonName, kindId, cancellationToken);
            InvalidateCachedRatingKinds(hackathonName);
        }
        #endregion

        #region CreateRatingAsync
        public async Task<RatingEntity> CreateRatingAsync(Rating parameter, CancellationToken cancellationToken)
        {
            var ratingEntity = new RatingEntity
            {
                PartitionKey = parameter.hackathonName,
                RowKey = GenerateRatingEntityRowKey(parameter.judgeId, parameter.teamId, parameter.ratingKindId),
                CreatedAt = DateTime.UtcNow,
                Description = parameter.description,
                JudgeId = parameter.judgeId,
                RatingKindId = parameter.ratingKindId,
                Score = parameter.score.GetValueOrDefault(0),
                TeamId = parameter.teamId,
            };

            await StorageContext.RatingTable.InsertOrMergeAsync(ratingEntity, cancellationToken);
            return ratingEntity;
        }
        #endregion

        #region UpdateRatingAsync
        public async Task<RatingEntity> UpdateRatingAsync(RatingEntity existing, Rating parameter, CancellationToken cancellationToken)
        {
            existing.Score = parameter.score.GetValueOrDefault(existing.Score);
            existing.Description = parameter.description ?? existing.Description;

            await StorageContext.RatingTable.MergeAsync(existing, cancellationToken);
            return existing;
        }
        #endregion

        #region GetRatingAsync
        public async Task<RatingEntity?> GetRatingAsync(string hackathonName, string judgeId, string teamId, string kindId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName)
                || string.IsNullOrWhiteSpace(judgeId)
                || string.IsNullOrWhiteSpace(teamId)
                || string.IsNullOrWhiteSpace(kindId))
                return null;

            var rowKey = GenerateRatingEntityRowKey(judgeId, teamId, kindId);
            return await StorageContext.RatingTable.RetrieveAsync(hackathonName, rowKey, cancellationToken);
        }
        #endregion

        #region GetRatingAsync
        public async Task<RatingEntity?> GetRatingAsync(string hackathonName, string ratingId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(ratingId))
                return null;

            return await StorageContext.RatingTable.RetrieveAsync(hackathonName, ratingId, cancellationToken);
        }
        #endregion

        #region IsRatingCountGreaterThanZero
        public async Task<bool> IsRatingCountGreaterThanZero(string hackathonName, RatingQueryOptions options, CancellationToken cancellationToken)
        {
            var filters = new List<string>();
            filters.Add(TableQueryHelper.PartitionKeyFilter(hackathonName));
            if (!string.IsNullOrWhiteSpace(options.JudgeId))
            {
                filters.Add(TableQueryHelper.FilterForString(nameof(RatingEntity.JudgeId), ComparisonOperator.Equal, options.JudgeId));
            }
            if (!string.IsNullOrWhiteSpace(options.RatingKindId))
            {
                filters.Add(TableQueryHelper.FilterForString(nameof(RatingEntity.RatingKindId), ComparisonOperator.Equal, options.RatingKindId));
            }
            if (!string.IsNullOrWhiteSpace(options.TeamId))
            {
                filters.Add(TableQueryHelper.FilterForString(nameof(RatingEntity.TeamId), ComparisonOperator.Equal, options.TeamId));
            }

            var filter = TableQueryHelper.And(filters.ToArray());
            var select = new[] { nameof(RatingEntity.RowKey) };
            var list = await StorageContext.RatingTable.QueryEntitiesAsync(filter, select, cancellationToken);

            return list.Count() > 0;
        }
        #endregion

        #region ListPaginatedRatingsAsync
        public async Task<Page<RatingEntity>> ListPaginatedRatingsAsync(string hackathonName, RatingQueryOptions options, CancellationToken cancellationToken = default)
        {
            var filters = new List<string>();
            filters.Add(TableQueryHelper.PartitionKeyFilter(hackathonName));
            if (!string.IsNullOrWhiteSpace(options.JudgeId))
            {
                filters.Add(TableQueryHelper.FilterForString(nameof(RatingEntity.JudgeId), ComparisonOperator.Equal, options.JudgeId));
            }
            if (!string.IsNullOrWhiteSpace(options.RatingKindId))
            {
                filters.Add(TableQueryHelper.FilterForString(nameof(RatingEntity.RatingKindId), ComparisonOperator.Equal, options.RatingKindId));
            }
            if (!string.IsNullOrWhiteSpace(options.TeamId))
            {
                filters.Add(TableQueryHelper.FilterForString(nameof(RatingEntity.TeamId), ComparisonOperator.Equal, options.TeamId));
            }
            var filter = TableQueryHelper.And(filters.ToArray());

            var page = await StorageContext.RatingTable.ExecuteQuerySegmentedAsync(filter, options.ContinuationToken(), options.Top(), null, cancellationToken);
            return page;
        }
        #endregion

        #region DeleteRatingAsync
        public async Task DeleteRatingAsync(string hackathonName, string ratingId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(hackathonName) || string.IsNullOrWhiteSpace(ratingId))
                return;

            await StorageContext.RatingTable.DeleteAsync(hackathonName, ratingId, cancellationToken);
        }
        #endregion

        private string GenerateRatingEntityRowKey(string judgeId, string teamId, string kindId)
        {
            string input = $"{judgeId}-{teamId}-{kindId}".ToLower();
            var guid = DigestHelper.String2Guid(input);
            return guid.ToString();
        }
    }
}
