using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Cache
{
    public class RedisCacheProvider : ICacheProvider
    {
        readonly string password;
        readonly ILogger<RedisCacheProvider> logger;

        public RedisCacheProvider(string password, ILogger<RedisCacheProvider> logger)
        {
            this.logger = logger;
            this.password = password;
        }

        public bool ContainsKey(string cacheKey)
        {
            throw new System.NotImplementedException();
        }

        public Task<TValue> GetOrAddAsync<TValue>(CacheEntry<TValue> cacheEntry, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task RefreshAllAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task RefreshAsync(string cacheKey, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public object? Remove(string key)
        {
            throw new System.NotImplementedException();
        }
    }
}
