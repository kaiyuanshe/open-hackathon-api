using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Cache
{
    public interface ICacheProviderFactory
    {
        ICacheProvider CreateCacheProvider();
    }

    public class CacheProviderFactory : ICacheProviderFactory
    {
        // if redis is enabled, a password is required. 
        readonly bool redisEnabled;
        readonly string redisPassword;
        readonly ILoggerFactory loggerFactory;

        public CacheProviderFactory(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            this.loggerFactory = loggerFactory;

            redisEnabled = configuration.GetValue<bool>(ConfigurationKeys.RedisCacheEnabled);
            redisPassword = configuration.GetValue<string>(ConfigurationKeys.RedisCachePassword);
        }

        public ICacheProvider CreateCacheProvider()
        {
            if (redisEnabled)
            {
                var logger = loggerFactory.CreateLogger<RedisCacheProvider>();
                return new RedisCacheProvider(redisPassword, logger);
            }
            else
            {
                var logger = loggerFactory.CreateLogger<DefaultCacheProvider>();
                return new DefaultCacheProvider(logger);
            }
        }
    }
}
