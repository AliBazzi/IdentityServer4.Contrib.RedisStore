using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityServer4.Contrib.RedisStore.Stores;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityServer4.Contrib.RedisStore.Tests.Stores
{
    public class PersistedGrantStoreTests
    {
        private readonly PersistedGrantStore _store;
        private readonly RedisMultiplexer<RedisOperationalStoreOptions> _multiplexer;
        private readonly Mock<ILogger<PersistedGrantStore>> _logger;
        private readonly Mock<ISystemClock> _clock;

        public PersistedGrantStoreTests()
        {
            _logger = new Mock<ILogger<PersistedGrantStore>>();
            _clock = new Mock<ISystemClock>();
            string connectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"];
            var options = new RedisOperationalStoreOptions { RedisConnectionString = connectionString };
            _multiplexer = new RedisMultiplexer<RedisOperationalStoreOptions>(options);

            _store = new PersistedGrantStore(_multiplexer, _logger.Object, _clock.Object);
        }

        [Fact]
        public void PersistedGrantStore_Null_Multiplexer_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PersistedGrantStore(null, _logger.Object, _clock.Object));
        }

        [Fact]
        public void PersistedGrantStore_Null_Logger_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PersistedGrantStore(_multiplexer, null, _clock.Object));
        }

        [Fact]
        public async Task StoreAysnc_Stores_Entries()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string key = nameof(StoreAysnc_Stores_Entries);
            string expected = "this is a test";
            var grant = new PersistedGrant { Key = key, Data = expected, Expiration = now.AddSeconds(1) };
            await _store.StoreAsync(grant);

            var actual = await _store.GetAsync(key);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual.Data);
        }

        [Fact]
        public async Task GetAsync_Does_Not_Return_Expired_Entries()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string key = $"{nameof(GetAsync_Does_Not_Return_Expired_Entries)}-{now:O}";
            string expected = "this is a test";
            var grant = new PersistedGrant { Key = key, Data = expected, Expiration = now.AddSeconds(1) };
            await _store.StoreAsync(grant);

            var actual = await _store.GetAsync(key);

            Assert.Equal(expected, actual.Data);

            Thread.Sleep(TimeSpan.FromSeconds(2));
            actual = await _store.GetAsync(key);

            Assert.Null(actual);
        }

        [Fact]
        public async Task GetAllAsync_Retrieves_All_Grants_For_SubjectId()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2)
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(subjectId)).ToList();

            Assert.NotNull(actual);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetAllAsync_Does_Not_Retrieve_Expired_Grants()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Does_Not_Retrieve_Expired_Grants)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Does_Not_Retrieve_Expired_Grants)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(-1)
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(subjectId)).ToList();

            Assert.NotNull(actual);
            Assert.Empty(actual);
        }
    }
}
