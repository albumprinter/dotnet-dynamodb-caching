using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Albelli.Extensions.Caching.DynamoDb
{
    public static class DynamoDbCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddStackExchangeRedisCache(this IServiceCollection services, Action<DynamoDbCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }
            services.AddOptions();
            services.Configure(setupAction);
            services.AddSingleton<ISystemClock, DefaultSystemClock>();
            services.AddSingleton<IDistributedCache, DynamoDbCache>();
            return services;
        }
    }
}