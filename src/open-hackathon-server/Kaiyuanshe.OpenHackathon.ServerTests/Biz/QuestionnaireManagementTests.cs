using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class QuestionnaireManagementTests
    {
        #region CreateQuestionnaireAsync
        [Test]
        public async Task CreateQuestionnaireAsync_Ok()
        {
            var hackathonName = "hack";
            var request = new Questionnaire
            {
                hackathonName = hackathonName,
            };

            var moqs = new Moqs();
            moqs.QuestionnaireTable.Setup(o => o.InsertAsync(It.Is<QuestionnaireEntity>(e =>
                e.HackathonName == request.hackathonName), default));
            moqs.CacheProvider.Setup(c => c.Remove(CacheKeys.GetCacheKey(CacheEntryType.Questionnaire, hackathonName)));

            var management = new QuestionnaireManagement();
            moqs.SetupManagement(management);
            await management.CreateQuestionnaireAsync(request, default);

            moqs.VerifyAll();
        }
        #endregion

        #region UpdateQuestionnaireAsync
        [Test]
        public async Task UpdateQuestionnaireAsync_Ok()
        {
            var hackathonName = "hack";
            var entity = new QuestionnaireEntity
            {
                PartitionKey = hackathonName,
            };
            var request = new Questionnaire
            {
                extensions = new Extension[]
                {
                    new Extension { name = "n", value = "v" },
                }
            };
            var expectedExtensions = request.extensions.Merge(null);

            var moqs = new Moqs();
            moqs.QuestionnaireTable.Setup(o => o.MergeAsync(It.Is<QuestionnaireEntity>(e =>
                hackathonName == e.HackathonName
                && Enumerable.SequenceEqual(expectedExtensions, e.Extensions)), default));
            moqs.CacheProvider.Setup(c => c.Remove(CacheKeys.GetCacheKey(CacheEntryType.Questionnaire, hackathonName)));

            var management = new QuestionnaireManagement();
            moqs.SetupManagement(management);
            await management.UpdateQuestionnaireAsync(entity, request, default);

            moqs.VerifyAll();
        }
        #endregion

        #region GetQuestionnaireAsync
        [Test]
        public async Task GetQuestionnaireAsync_Ok()
        {
            var hackathonName = "hack";

            var moqs = new Moqs();
            moqs.QuestionnaireTable.Setup(o => o.RetrieveAsync(hackathonName, string.Empty, default));

            var management = new QuestionnaireManagement();
            moqs.SetupManagement(management);
            await management.GetQuestionnaireAsync(hackathonName, default);

            moqs.VerifyAll();
        }
        #endregion

        #region DeleteQuestionnaireAsync
        [Test]
        public async Task DeleteQuestionnaireAsync_Ok()
        {
            var hackathonName = "hack";

            var moqs = new Moqs();
            moqs.QuestionnaireTable.Setup(o => o.DeleteAsync(hackathonName, string.Empty, default));
            moqs.CacheProvider.Setup(c => c.Remove(CacheKeys.GetCacheKey(CacheEntryType.Questionnaire, hackathonName)));

            var management = new QuestionnaireManagement();
            moqs.SetupManagement(management);
            await management.DeleteQuestionnaireAsync(hackathonName, default);

            moqs.VerifyAll();
        }
        #endregion
    }
}
