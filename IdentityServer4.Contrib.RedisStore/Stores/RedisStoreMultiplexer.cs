using StackExchange.Redis;

namespace IdentityServer4.Contrib.RedisStore.Stores
{
    public class RedisStoreMultiplexer
    {
        private readonly IConnectionMultiplexer multiplexer;

        private readonly int DB;

        public RedisStoreMultiplexer(string connectionString, int DB = 0)
        {
            this.DB = DB;
            this.multiplexer = ConnectionMultiplexer.Connect(connectionString);
        }

        public IDatabase GetDatabase()
        {
            return this.multiplexer.GetDatabase(this.DB);
        }
    }
}
