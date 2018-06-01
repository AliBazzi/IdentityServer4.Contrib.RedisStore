using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Contrib.RedisStore.Cache;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityServer4.Contrib.RedisStore.Tests.Cache
{
    public class RedisCacheTests
    {
        private readonly RedisCache<string> _cache;

        public RedisCacheTests()
        {
            var logger = new Mock<ILogger<RedisCache<string>>>();
            var options = new RedisCacheOptions { RedisConnectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"] };
            var multiplexer = new RedisMultiplexer<RedisCacheOptions>(options);

            _cache = new RedisCache<string>(multiplexer, logger.Object);
        }

        [Fact]
        public void RedisCache_Null_Multiplexer_Throws_ArgumentNullException()
        {
            var logger = new Mock<ILogger<RedisCache<string>>>();

            Assert.Throws<ArgumentNullException>(() => new RedisCache<string>(null, logger.Object));
        }

        [Fact]
        public void RedisCache_Null_Logger_Throws_ArgumentNullException()
        {
            var multiplexer = new RedisMultiplexer<RedisCacheOptions>(new RedisCacheOptions { RedisConnectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"] });

            Assert.Throws<ArgumentNullException>(() => new RedisCache<string>(multiplexer, null));
        }

        [Fact]
        public async Task SetAsync_Stores_Entries()
        {
            string key = nameof(SetAsync_Stores_Entries);
            string expected = "test_value";
            await _cache.SetAsync(key, expected, TimeSpan.FromSeconds(1));

            var actual = await _cache.GetAsync(key);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GetAsync_Does_Not_Return_Expired_Entries()
        {
            string key = nameof(GetAsync_Does_Not_Return_Expired_Entries);
            string expected = "test_value";
            await _cache.SetAsync(key, expected, TimeSpan.FromSeconds(1));

            var actual = await _cache.GetAsync(key);
            Assert.Equal(expected, actual);

            Thread.Sleep(1500);

            actual = await _cache.GetAsync(key);

            Assert.Null(actual);
        }
    }
}
