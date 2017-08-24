using IdentityServer4.Contrib.RedisStore.Stores;
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
        /// <param name="db">the number of Db in Redis Instance, default is 0</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, string redisConnectionString, int db = -1)
        {
            builder.Services.AddSingleton(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.AddScoped<IDatabaseAsync>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase(db));
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
            builder.Services.AddSingleton(_ => ConnectionMultiplexer.Connect(options));
            builder.Services.AddScoped<IDatabaseAsync>(_ => _.GetRequiredService<IConnectionMultiplexer>().GetDatabase(options.DefaultDatabase.HasValue ? options.DefaultDatabase.Value : -1));
            builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
            return builder;
        }
    }
}
