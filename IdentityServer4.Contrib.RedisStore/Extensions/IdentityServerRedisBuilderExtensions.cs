using IdentityServer4.Contrib.RedisStore;
using IdentityServer4.Contrib.RedisStore.Cache;
using IdentityServer4.Contrib.RedisStore.Stores;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServerRedisBuilderExtensions
    {
        /// <summary>
        /// Add Redis Operational Store.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder">Redis Configuration Store Options builder</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, Action<RedisConfigurationStoreOptions> optionsBuilder)
        {
            var options = new RedisConfigurationStoreOptions();
            optionsBuilder?.Invoke(options);
            options.Connect();

            builder.Services.TryAddSingleton(options);
            builder.Services.TryAddScoped<RedisMultiplexer<RedisConfigurationStoreOptions>>();
            builder.Services.TryAddTransient<IPersistedGrantStore, PersistedGrantStore>();
            return builder;
        }

        /// <summary>
        /// Add Redis caching that implements ICache<typeparamref name="T"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder">Redis Cache Options builder</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddRedisCaching(this IIdentityServerBuilder builder, Action<RedisCacheOptions> optionsBuilder)
        {
            var options = new RedisCacheOptions();
            optionsBuilder?.Invoke(options);
            options.Connect();

            builder.Services.TryAddSingleton(options);
            builder.Services.TryAddScoped<RedisMultiplexer<RedisCacheOptions>>();
            builder.Services.TryAddTransient(typeof(ICache<>), typeof(RedisCache<>));
            return builder;
        }
    }
}
