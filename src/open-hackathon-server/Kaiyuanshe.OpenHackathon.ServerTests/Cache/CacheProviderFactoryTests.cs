using Kaiyuanshe.OpenHackathon.Server.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Cache
{
    internal class CacheProviderFactoryTests
    {
        [TestCase(false, typeof(DefaultCacheProvider))]
        [TestCase(true, typeof(RedisCacheProvider))]
        public void CreateCacheProvider(bool redisEnabled, Type expectedProviderType)
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"Redis:Enabled", redisEnabled.ToString()},
                {"Redis:Password", ""},
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var loggerFactory = new Mock<ILoggerFactory>();

            var factory = new CacheProviderFactory(loggerFactory.Object, configuration);
            var cacheProvider = factory.CreateCacheProvider();

            Assert.AreEqual(expectedProviderType, cacheProvider.GetType());
        }
    }
}
