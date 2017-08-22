using IdentityServer4.Contrib.RedisStore.Stores;
using IdentityServer4.Stores;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServerRedisBuilderExtensions
    {
        /// <summary>
        /// Add Redis Store Operational Store.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="redisConnectionString">Redis Store Connection String</param>
        /// <param name="db">the number of Db in Redis Instance, default is 0</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(
            this IIdentityServerBuilder builder, string redisConnectionString, int db = 0)
        {
            builder.Services.AddSingleton(_ => new RedisStoreMultiplexer(redisConnectionString, db));
            builder.Services.AddScoped<IDatabaseAsync>(_ => _.GetRequiredService<RedisStoreMultiplexer>().GetDatabase());
            builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
            return builder;
        }
    }
}
