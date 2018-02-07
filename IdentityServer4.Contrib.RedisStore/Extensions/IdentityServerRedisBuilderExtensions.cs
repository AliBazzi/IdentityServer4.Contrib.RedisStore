using IdentityServer4.Contrib.RedisStore.Cache;
using IdentityServer4.Contrib.RedisStore.Stores;
using IdentityServer4.Services;
using IdentityServer4.Stores;
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
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.AddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase(db));
            builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
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
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.AddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
            builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
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
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.AddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase(db));
            builder.Services.AddTransient(typeof(ICache<>), typeof(RedisCache<>));
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
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.AddScoped<IDatabase>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
            builder.Services.AddTransient(typeof(ICache<>), typeof(RedisCache<>));
            return builder;
        }
    }
}
