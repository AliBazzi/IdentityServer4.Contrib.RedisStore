using IdentityServer4.Contrib.RedisStore.Cache;
using IdentityServer4.Contrib.RedisStore.Stores;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServerRedisBuilderExtensions
    {
        /// <summary>
        /// Add Redis Operational Store.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="redisConnectionString">Redis Store Connection String</param>
        /// <param name="db">the number of Db in Redis Instance</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, string redisConnectionString, int db = -1)
        {
            builder.Services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.TryAddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase(db));
            builder.Services.TryAddTransient<IPersistedGrantStore, PersistedGrantStore>();
            return builder;
        }

        /// <summary>
        /// Add Redis Operational Store.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">ConfigurationOptions object.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, ConfigurationOptions options)
        {
            builder.Services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.TryAddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
            builder.Services.TryAddTransient<IPersistedGrantStore, PersistedGrantStore>();
            return builder;
        }

        /// <summary>
        /// Add Redis caching that implements <typeparamref name="T"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="redisConnectionString">Redis store connection string</param>
        /// <param name="db">the number of Db in Redis Instance</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddRedisCaching(this IIdentityServerBuilder builder, string redisConnectionString, int db = -1)
        {
            builder.Services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.TryAddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase(db));
            builder.Services.TryAddTransient(typeof(ICache<>), typeof(RedisCache<>));
            return builder;
        }

        /// <summary>
        /// Add Redis caching that implements ICache<typeparamref name="T"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">ConfigurationOptions object.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddRedisCaching(this IIdentityServerBuilder builder, ConfigurationOptions options)
        {
            builder.Services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.TryAddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
            builder.Services.TryAddTransient(typeof(ICache<>), typeof(RedisCache<>));
            return builder;
        }
    }
}
