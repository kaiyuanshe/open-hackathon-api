﻿using Kaiyuanshe.OpenHackathon.Server.Cache;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Cache
{
    public class DefaultCacheProviderTests
    {
        [Test]
        public void ContainsKey()
        {
            var cacheProvider = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            Assert.IsFalse(cacheProvider.ContainsKey("abc"));
            MemoryCache.Default.Add("abc", "val", DateTimeOffset.Now.AddMinutes(2));
            Assert.IsTrue(cacheProvider.ContainsKey("abc"));
            MemoryCache.Default.Remove("abc");
            Assert.IsFalse(cacheProvider.ContainsKey("abc"));
        }

        [Test]
        public async Task GetOrAddAsync()
        {
            CacheEntry<string> entry = new CacheEntry<string>(
                "GetOrAddAsync",
                TimeSpan.FromDays(1),
                (token) => { return Task.FromResult("second"); },
                false
                );
            CancellationToken cancellationToken = CancellationToken.None;

            MemoryCache.Default.Add(entry.CacheKey, "first", entry.CachePolicy);
            var cacheProvider = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            string value = await cacheProvider.GetOrAddAsync(entry, cancellationToken);
            Assert.AreEqual("second", value);
        }

        [Test]
        public async Task RefreshAllAsync()
        {
            // not autofresh
            CacheEntry<string> first = new CacheEntry<string>(
                "first",
                TimeSpan.FromDays(1),
                (token) => { return Task.FromResult("first"); },
                false
                );
            // existing in cache
            CacheEntry<string> second = new CacheEntry<string>(
                "second",
                TimeSpan.FromDays(1),
                (token) => { return Task.FromResult("second"); },
                true
                );
            // refreshed
            CacheEntry<string> third = new CacheEntry<string>(
                "third",
                TimeSpan.FromDays(1),
                (token) => { return Task.FromResult("third"); },
                true
                );
            CancellationToken cancellationToken = CancellationToken.None;

            var cacheProvider = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            await cacheProvider.GetOrAddAsync(first, cancellationToken);
            await cacheProvider.GetOrAddAsync(second, cancellationToken);
            await cacheProvider.GetOrAddAsync(third, cancellationToken);
            Assert.IsFalse(MemoryCache.Default.Contains(first.CacheKey));
            Assert.IsFalse(MemoryCache.Default.Contains(second.CacheKey));
            Assert.IsFalse(MemoryCache.Default.Contains(third.CacheKey));

            MemoryCache.Default.Add(second.CacheKey, "second-value", second.CachePolicy);
            await cacheProvider.RefreshAllAsync(cancellationToken);

            Assert.IsFalse(MemoryCache.Default.Contains(first.CacheKey));
            Assert.IsTrue(MemoryCache.Default.Contains(second.CacheKey));
            Assert.AreEqual("second-value", (string)MemoryCache.Default.Get(second.CacheKey));
            Assert.IsTrue(MemoryCache.Default.Contains(third.CacheKey));
            Assert.AreEqual("third", (string)MemoryCache.Default.Get(third.CacheKey));
        }

        [Test]
        public async Task RefreshAsync()
        {
            CacheEntry<string> entry = new CacheEntry<string>(
                "RefreshAsync",
                TimeSpan.FromDays(1),
                (token) => { return Task.FromResult("t3"); },
                false
                );
            CancellationToken cancellationToken = CancellationToken.None;

            var cacheProvider = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            await cacheProvider.GetOrAddAsync(entry, cancellationToken);
            Assert.IsFalse(MemoryCache.Default.Contains(entry.CacheKey));

            await cacheProvider.RefreshAsync(entry.CacheKey, cancellationToken);
            Assert.IsTrue(MemoryCache.Default.Contains(entry.CacheKey));
            Assert.AreEqual("t3", (string)MemoryCache.Default.Get(entry.CacheKey));

            await cacheProvider.RefreshAsync("unknown", cancellationToken);
            Assert.IsFalse(MemoryCache.Default.Contains("unknown"));
        }

        [Test]
        public async Task GetOrAddInternalTest()
        {
            CacheEntry<string> entry = new CacheEntry<string>(
               "internal",
               TimeSpan.FromDays(1),
               (token) =>
               {
                   return Task.FromResult("first");
               }, false);
            CancellationToken cancellationToken = CancellationToken.None;

            var cacheProvider = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);

            MemoryCache.Default.Add(entry.CacheKey, "existing", entry.CachePolicy);
            string value = (string)await cacheProvider.GetOrAddAsyncInternal(entry, cancellationToken);
            Assert.AreEqual("existing", value);

            MemoryCache.Default.Remove(entry.CacheKey);
            value = (string)await cacheProvider.GetOrAddAsyncInternal(entry, cancellationToken);
            Assert.AreEqual("first", value);
        }
    }
}
