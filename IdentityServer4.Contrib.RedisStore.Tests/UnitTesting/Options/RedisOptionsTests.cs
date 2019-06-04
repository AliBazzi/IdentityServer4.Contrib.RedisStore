using IdentityServer4.Contrib.RedisStore.Tests.Fakes;
using Xunit;

namespace IdentityServer4.Contrib.RedisStore.Tests.Options
{
    public class RedisOptionsTests
    {
        [Fact]
        public void Multiplexer_Provided_Uses_Provided_Multiplexer()
        {
            var cacheOptions = new RedisCacheOptions()
            {
                RedisConnectionMultiplexer = new FakeConnectionMultiplexer()
            };

            Assert.IsType<FakeConnectionMultiplexer>(cacheOptions.RedisConnectionMultiplexer);

            var storeOptions = new RedisOperationalStoreOptions()
            {
                RedisConnectionMultiplexer = new FakeConnectionMultiplexer()
            };

            Assert.IsType<FakeConnectionMultiplexer>(storeOptions.RedisConnectionMultiplexer);
        }

        [Fact]
        public void Multiplexer_And_ConnectionString_Provided_Uses_Provided_Multiplexer()
        {
            var cacheOptions = new RedisCacheOptions()
            {
                RedisConnectionString = "fake", // if connection is made, this will throw
                RedisConnectionMultiplexer = new FakeConnectionMultiplexer()
            };

            Assert.IsType<FakeConnectionMultiplexer>(cacheOptions.RedisConnectionMultiplexer);

            var storeOptions = new RedisOperationalStoreOptions()
            {
                RedisConnectionString = "fake", // if connection is made, this will throw
                RedisConnectionMultiplexer = new FakeConnectionMultiplexer()
            };

            Assert.IsType<FakeConnectionMultiplexer>(storeOptions.RedisConnectionMultiplexer);
        }

        [Fact]
        public void ConnectionString_Provided_Makes_Connection()
        {
            var cacheOptions = new RedisCacheOptions()
            {
                RedisConnectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"]
            };

            Assert.IsType<StackExchange.Redis.ConnectionMultiplexer>(cacheOptions.RedisConnectionMultiplexer);

            var storeOptions = new RedisOperationalStoreOptions()
            {
                RedisConnectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"]
            };

            Assert.IsType<StackExchange.Redis.ConnectionMultiplexer>(storeOptions.RedisConnectionMultiplexer);
        }
    }
}
