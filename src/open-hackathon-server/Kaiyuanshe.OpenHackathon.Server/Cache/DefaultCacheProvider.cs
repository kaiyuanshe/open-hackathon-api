using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Cache
{
    public class DefaultCacheProvider : ICacheProvider
    {
        static MemoryCache cache = MemoryCache.Default;
        static Dictionary<string, CacheEntry> cacheEntries = new Dictionary<string, CacheEntry>();
        private readonly ILogger Logger;

        public DefaultCacheProvider(ILogger<DefaultCacheProvider> logger)
        {
            Logger = logger;
        }

        public bool ContainsKey(string cacheKey)
        {
            return cache.Contains(cacheKey);
        }

        public async Task<TValue> GetOrAddAsync<TValue>(CacheEntry<TValue> cacheEntry, CancellationToken cancellationToken)
        {
            cacheEntries[cacheEntry.CacheKey] = cacheEntry;

            // disable cache in Dev or Unit Tests
            if (EnvironmentHelper.IsDevelopment() || EnvironmentHelper.IsRunningInTests())
            {
                return (TValue)await cacheEntry.SupplyValueAsync(cancellationToken);
            }

            return (TValue)await GetOrAddAsyncInternal(cacheEntry, cancellationToken);
        }

        public async Task RefreshAllAsync(CancellationToken cancellationToken)
        {
            foreach (var cacheEntry in cacheEntries.Values)
            {
                if (!cacheEntry.AutoRefresh)
                {
                    continue;
                }

                if (cache.Contains(cacheEntry.CacheKey))
                {
                    continue;
                }

                Logger?.LogInformation($"Refreshing entry in cache: {cacheEntry.CacheKey}");
                var value = await cacheEntry.SupplyValueAsync(cancellationToken);
                if (value != null)
                {
                    cache.Add(cacheEntry.CacheKey, value, cacheEntry.CachePolicy);
                }
            }
        }

        public async Task RefreshAsync(string cacheKey, CancellationToken cancellationToken)
        {
            if (cacheEntries.TryGetValue(cacheKey, out CacheEntry? cacheEntry))
            {
                var value = await cacheEntry.SupplyValueAsync(cancellationToken);
                if (value != null)
                {
                    cache.Add(cacheEntry.CacheKey, value, cacheEntry.CachePolicy);
                }
            }
        }

        public object? Remove(string key)
        {
            if (cache.Contains(key))
            {
                return cache.Remove(key);
            }

            return null;
        }

        internal async Task<object> GetOrAddAsyncInternal(CacheEntry cacheEntry, CancellationToken cancellationToken)
        {
            if (!cache.Contains(cacheEntry.CacheKey))
            {
                var value = await cacheEntry.SupplyValueAsync(cancellationToken);
                if (value != null)
                {
                    cache.Add(cacheEntry.CacheKey, value, cacheEntry.CachePolicy);
                    return value;
                }
                else
                {
#pragma warning disable CS8603 // Possible null reference return.
                    return value;
#pragma warning restore CS8603 // Possible null reference return.
                }
            }
            else
            {
                return cache[cacheEntry.CacheKey];
            }
        }
    }

    public static class CacheProviderExtension
    {
        public static Task<TValue> GetOrAddAsync<TValue>(
            this ICacheProvider cache,
            string cacheKey,
            TimeSpan slidingExpiration,
            Func<CancellationToken, Task<TValue>> supplyVaule,
            bool autoRefresh = false,
            CancellationToken cancellationToken = default)
        {
            var entry = new CacheEntry<TValue>(cacheKey, slidingExpiration, supplyVaule, autoRefresh);
            return cache.GetOrAddAsync(entry, cancellationToken);
        }
    }
}
