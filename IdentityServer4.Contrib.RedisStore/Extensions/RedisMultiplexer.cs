using StackExchange.Redis;

namespace IdentityServer4.Contrib.RedisStore
{
    /// <summary>
    /// represents Redis general multiplexer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisMultiplexer<T> where T : RedisOptions
    {
        public RedisMultiplexer(T redisOptions)
        {
            this.RedisOptions = redisOptions;
            this.GetDatabase();
        }

        private void GetDatabase()
        {
            this.Database = this.RedisOptions.Multiplexer.GetDatabase(string.IsNullOrEmpty(this.RedisOptions.RedisConnectionString) ? -1 : this.RedisOptions.Db);
        }

        internal T RedisOptions { get; }

        internal IDatabase Database { get; private set; }
    }
}
