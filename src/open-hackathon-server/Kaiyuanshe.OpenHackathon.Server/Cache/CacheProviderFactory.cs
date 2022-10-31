using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Cache
{
    public interface ICacheProviderFactory
    {
        ICacheProvider CreateCacheProvider();
    }

    public class CacheProviderFactory : ICacheProviderFactory
    {
        readonly ILoggerFactory loggerFactory;

        public CacheProviderFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public ICacheProvider CreateCacheProvider()
        {
            var logger = loggerFactory.CreateLogger<DefaultCacheProvider>();
            return new DefaultCacheProvider(logger);
        }
    }
}
