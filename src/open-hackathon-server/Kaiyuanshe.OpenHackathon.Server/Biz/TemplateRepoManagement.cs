using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface ITemplateRepoManagement
    {
        Task<TemplateRepoEntity> CreateTemplateRepoAsync(TemplateRepo request, UserInfo userInfo, CancellationToken cancellationToken);
        Task<TemplateRepoEntity> UpdateTemplateRepoAsync(TemplateRepoEntity existing, TemplateRepo request, UserInfo userInfo, CancellationToken cancellationToken);
        Task<TemplateRepoEntity?> GetTemplateRepoAsync([DisallowNull] string hackathonName, [DisallowNull] string templateRepoId, CancellationToken cancellationToken);
        Task<IEnumerable<TemplateRepoEntity>> ListPaginatedTemplateReposAsync([DisallowNull] string hackathonName, TemplateRepoQueryOptions options, CancellationToken cancellationToken = default);
        Task DeleteTemplateRepoAsync([DisallowNull] string hackathonName, [DisallowNull] string templateRepoId, CancellationToken cancellationToken);
    }

    public class TemplateRepoManagement : ManagementClient<TemplateRepoManagement>, ITemplateRepoManagement
    {
        private static Regex regexGitHubRepoUrl = new Regex(@"(https:\/\/github\.com\/([A-Za-z0-9_\-]+)\/([A-Za-z0-9_\-]+))\/?");

        #region Cache
        private string CacheKeyByHackathon(string hackathonName)
        {
            return CacheKeys.GetCacheKey(CacheEntryType.TemplateRepo, hackathonName);
        }

        private void InvalidateCachedTemplateRepos(string hackathonName)
        {
            Cache.Remove(CacheKeyByHackathon(hackathonName));
        }

        private async Task<IEnumerable<TemplateRepoEntity>> GetCachedTemplateRepos(string hackathonName, CancellationToken cancellationToken)
        {
            string cacheKey = CacheKeyByHackathon(hackathonName);
            return await Cache.GetOrAddAsync(cacheKey, TimeSpan.FromHours(6), (ct) =>
            {
                return StorageContext.TemplateRepoTable.ListByHackathonAsync(hackathonName, ct);
            }, false, cancellationToken);
        }
        #endregion

        private static string? GetGitHubPAT(UserInfo userInfo) {
            return userInfo.Identities
                .Where(e => e.Provider == "github")
                .Select(e => e.AccessToken)
                .First();
        }

        private async Task<JObject> FetchGitHubRestApi(HttpClient httpClient, string url, CancellationToken cancellationToken = default)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                using (var textReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                using (var jsonReader = new JsonTextReader(textReader))
                return await JObject.LoadAsync(jsonReader, cancellationToken);
            }
        }

        private async Task FetchGitHubInfo(TemplateRepo templateRepo, string? pat, CancellationToken cancellationToken)
        {
            // Normalize the url.
            var match = regexGitHubRepoUrl.Match(templateRepo.url);
            if (!match.Success) {
                Logger?.TraceInformation($"Url not matched: ${templateRepo.url}");
                return;
            }
            templateRepo.url = match.Groups[1].Value; // without trailing "/"
            var owner = match.Groups[2].Value;
            var repo = match.Groups[3].Value;

            if (templateRepo.isFetched == true) {
                return;
            }

            if (pat == null) {
                //return;
            }

            using (var httpClient = HttpClientFactory.CreateClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("dotnet")));
                httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                try {
                    // https://docs.github.com/en/rest/repos/repos#list-repository-languages
                    string url = string.Format("https://api.github.com/repos/{0}/{1}/languages", owner, repo);
                    var response = await FetchGitHubRestApi(httpClient, url, cancellationToken);
                    var map = (response as IEnumerable<KeyValuePair<string, JToken?>>).ToDictionary(
                        kvp => kvp.Key,
                        kvp => Convert.ToString((ulong)kvp.Value));
                    templateRepo.repoLanguages = map;
                }
                catch (Exception e) {
                    Logger?.TraceError($"Internal error: {e.Message}", e);
                }

                try {
                    // https://docs.github.com/en/rest/repos/repos#get-all-repository-topics
                    string url = string.Format("https://api.github.com/repos/{0}/{1}/topics", owner, repo);
                    var response = await FetchGitHubRestApi(httpClient, url, cancellationToken);
                    var names = response["names"].Select(t => (string)t).ToList();
                    templateRepo.repoTopics = names;
                }
                catch (Exception e) {
                    Logger?.TraceError($"Internal error: {e.Message}", e);
                }
            }

            templateRepo.isFetched = true;
        }

        #region CreateTemplateRepoAsync
        public async Task<TemplateRepoEntity> CreateTemplateRepoAsync(TemplateRepo request, UserInfo userInfo, CancellationToken cancellationToken)
        {
            var pat = GetGitHubPAT(userInfo);
            await FetchGitHubInfo(request, pat, cancellationToken);

            var entity = new TemplateRepoEntity
            {
                PartitionKey = request.hackathonName,
                RowKey = Guid.NewGuid().ToString(),
                Url = request.url,
                CreatedAt = DateTime.UtcNow,
            };

            if (request.isFetched == true) {
                entity.IsFetched = true;
                entity.RepoLanguages = request.repoLanguages;
                entity.RepoTopics = request.repoTopics;
            }

            await StorageContext.TemplateRepoTable.InsertAsync(entity, cancellationToken);
            InvalidateCachedTemplateRepos(entity.HackathonName);

            return entity;
        }
        #endregion

        #region UpdateTemplateRepoAsync
        public async Task<TemplateRepoEntity> UpdateTemplateRepoAsync(
            TemplateRepoEntity existing, TemplateRepo request, UserInfo userInfo,
            CancellationToken cancellationToken)
        {
            if (request.url == null || string.Equals(existing.Url, request.url)) {
                request.url = existing.Url;
                request.isFetched = existing.IsFetched;
            }
            else {
                request.isFetched = false;
            }
            var pat = GetGitHubPAT(userInfo);
            await FetchGitHubInfo(request, pat, cancellationToken);

            if (request.isFetched == true) {
                existing.IsFetched = true;
                existing.RepoLanguages = request.repoLanguages;
                existing.RepoTopics = request.repoTopics;
            }

            existing.Url = request.url ?? existing.Url;
            await StorageContext.TemplateRepoTable.MergeAsync(existing, cancellationToken);
            InvalidateCachedTemplateRepos(existing.HackathonName);

            return existing;
        }
        #endregion

        #region GetTemplateRepoAsync
        public async Task<TemplateRepoEntity?> GetTemplateRepoAsync(
            [DisallowNull] string hackathonName, [DisallowNull] string templateRepoId,
            CancellationToken cancellationToken)
        {
            return await StorageContext.TemplateRepoTable.RetrieveAsync(hackathonName, templateRepoId, cancellationToken);
        }
        #endregion

        #region ListPaginatedTemplateReposAsync
        public async Task<IEnumerable<TemplateRepoEntity>> ListPaginatedTemplateReposAsync(
            [DisallowNull] string hackathonName, TemplateRepoQueryOptions options,
            CancellationToken cancellationToken = default)
        {
            var entities = await GetCachedTemplateRepos(hackathonName, cancellationToken);

            // paging
            int.TryParse(options.Pagination?.np, out int np);
            int top = options.Top();
            var organizers = entities.OrderByDescending(a => a.CreatedAt).Skip(np).Take(top);

            // next paging
            options.NextPage = null;
            if (np + top < entities.Count())
            {
                options.NextPage = new Pagination
                {
                    np = (np + top).ToString(),
                    nr = (np + top).ToString(),
                };
            }

            return organizers;
        }
        #endregion

        #region DeleteTemplateRepoAsync
        public async Task DeleteTemplateRepoAsync(
            [DisallowNull] string hackathonName, [DisallowNull] string templateRepoId,
            CancellationToken cancellationToken)
        {
            await StorageContext.TemplateRepoTable.DeleteAsync(hackathonName, templateRepoId, cancellationToken);
            InvalidateCachedTemplateRepos(hackathonName);
        }
        #endregion
    }
}
