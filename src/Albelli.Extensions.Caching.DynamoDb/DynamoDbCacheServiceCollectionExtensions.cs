using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Albelli.Extensions.Caching.DynamoDb
{
    public static class DynamoDbCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Use DynamoDb as a provider for the IDistributedCache.
        /// </summary>
        /// <param name="services">The service collection to add this to.</param>
        /// <param name="setupAction">The configuration object for this implementation.</param>
        /// <returns>A chain result with the same service collection.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddDynamoDbCache(this IServiceCollection services, Action<DynamoDbCacheOptions> setupAction)
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