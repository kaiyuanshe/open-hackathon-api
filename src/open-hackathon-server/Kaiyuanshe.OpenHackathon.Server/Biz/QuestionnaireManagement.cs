using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IQuestionnaireManagement
    {
        Task<QuestionnaireEntity> CreateQuestionnaireAsync(Questionnaire request, CancellationToken cancellationToken);
        Task<QuestionnaireEntity> UpdateQuestionnaireAsync(QuestionnaireEntity existing, Questionnaire request, CancellationToken cancellationToken);
        Task<QuestionnaireEntity?> GetQuestionnaireAsync([DisallowNull] string hackathonName, CancellationToken cancellationToken);
        Task DeleteQuestionnaireAsync(string hackathonName, CancellationToken cancellationToken);
    }

    public class QuestionnaireManagement : ManagementClient<QuestionnaireManagement>, IQuestionnaireManagement
    {
        #region Cache
        private string CacheKeyByHackathon(string hackathonName)
        {
            return CacheKeys.GetCacheKey(CacheEntryType.Questionnaire, hackathonName);
        }

        private void InvalidateCachedQuestionnaires(string hackathonName)
        {
            Cache.Remove(CacheKeyByHackathon(hackathonName));
        }

        private async Task<QuestionnaireEntity?> GetCachedQuestionnaire(string hackathonName, CancellationToken cancellationToken)
        {
            string cacheKey = CacheKeyByHackathon(hackathonName);
            return await Cache.GetOrAddAsync(cacheKey, TimeSpan.FromHours(6), (ct) =>
            {
                return GetQuestionnaireAsync(hackathonName, ct);
            }, false, cancellationToken);
        }
        #endregion

        #region CreateQuestionnaireAsync
        public async Task<QuestionnaireEntity> CreateQuestionnaireAsync(Questionnaire request, CancellationToken cancellationToken)
        {
            var entity = new QuestionnaireEntity
            {
                PartitionKey = request.hackathonName,
                RowKey = string.Empty,
                CreatedAt = DateTime.UtcNow,
                Extensions = request.extensions.Merge(null),
            };
            await StorageContext.QuestionnaireTable.InsertAsync(entity, cancellationToken);
            InvalidateCachedQuestionnaires(entity.HackathonName);

            return entity;
        }
        #endregion

        #region UpdateQuestionnaireAsync
        public async Task<QuestionnaireEntity> UpdateQuestionnaireAsync(QuestionnaireEntity existing, Questionnaire request, CancellationToken cancellationToken)
        {
            existing.Extensions = existing.Extensions.Merge(request.extensions);
            await StorageContext.QuestionnaireTable.MergeAsync(existing, cancellationToken);
            InvalidateCachedQuestionnaires(existing.HackathonName);

            return existing;
        }
        #endregion

        #region GetQuestionnaireAsync
        public async Task<QuestionnaireEntity?> GetQuestionnaireAsync(
            [DisallowNull] string hackathonName,
            CancellationToken cancellationToken)
        {
            return await StorageContext.QuestionnaireTable.RetrieveAsync(hackathonName, string.Empty, cancellationToken);
        }
        #endregion

        #region DeleteQuestionnaireAsync
        public async Task DeleteQuestionnaireAsync(string hackathonName, CancellationToken cancellationToken)
        {
            await StorageContext.QuestionnaireTable.DeleteAsync(hackathonName, string.Empty, cancellationToken);
            InvalidateCachedQuestionnaires(hackathonName);
        }
        #endregion
    }
}
