using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Cache
{
    public interface ICacheProvider
    {
        /// <summary>
        /// whether or not contains a cache key
        /// </summary>
        /// <returns></returns>
        bool ContainsKey(string cacheKey);

        /// <summary>
        /// Get or add cached value
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="cacheEntry"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<TValue> GetOrAddAsync<TValue>(CacheEntry<TValue> cacheEntry, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a cache entry from the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>If the entry is found in the cache, the removed cache entry; otherwise, null.</returns>
        public object? Remove(string key);

        /// <summary>
        /// Refresh the cached value. Ignored if CacheEntry is not found.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task RefreshAsync(string cacheKey, CancellationToken cancellationToken);

        /// <summary>
        /// Refresh all cached values if expired and AutoRefresh is enabled
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task RefreshAllAsync(CancellationToken cancellationToken);
    }
}
