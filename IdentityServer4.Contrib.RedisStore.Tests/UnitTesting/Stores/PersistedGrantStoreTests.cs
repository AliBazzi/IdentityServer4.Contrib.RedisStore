using FluentAssertions;
using IdentityServer4.Contrib.RedisStore.Stores;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task StoreAsync_Stores_Entries()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string key = nameof(StoreAsync_Stores_Entries);
            string expected = "this is a test";
            var grant = new PersistedGrant { Key = key, Data = expected, ClientId = "client1", SubjectId = "sub1", Type = "type1", Expiration = now.AddSeconds(1) };
            await _store.StoreAsync(grant);

            var actual = await _store.GetAsync(key);

            Assert.NotNull(actual);
            Assert.Equal(expected, actual.Data);
        }

        [Fact]
        public async Task Store_And_Remove_Entries()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string key = nameof(Store_And_Remove_Entries);
            string expected = "this is a test";
            var grant = new PersistedGrant { Key = key, Data = expected, ClientId = "client1", SubjectId = "sub1", Type = "type1", Expiration = now.AddSeconds(1) };
            await _store.StoreAsync(grant);

            await _store.RemoveAsync(key);

            var actual = await _store.GetAsync(key);

            Assert.Null(actual);
        }

        [Fact]
        public async Task RemoveAll_Entries()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(RemoveAll_Entries)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(RemoveAll_Entries)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = "client1",
                    Type = "type1",
                }
            ).ToList();

            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            await _store.RemoveAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1" });

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId })).ToList();

            Assert.Empty(actual);
        }

        [Fact]
        public async Task RemoveAll_Entries_With_SessionId()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(RemoveAll_Entries_With_SessionId)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(RemoveAll_Entries_With_SessionId)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = "client1",
                    Type = "type1",
                    SessionId = $"session{x}"
                }
            ).ToList();

            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            await _store.RemoveAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1", SessionId = "session1" });

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId })).ToList();

            actual.Should().HaveCount(4);
        }

        [Fact]
        public async Task RemoveAll_Entries_With_Type()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(RemoveAll_Entries_With_Type)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(RemoveAll_Entries_With_Type)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = "client1",
                    Type = x > 2 ? "type1" : "type2",
                    SessionId = $"session{x}"
                }
            ).ToList();

            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            await _store.RemoveAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1", Type = "type2" });

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId })).ToList();

            actual.Should().HaveCount(2);
        }

        [Fact]
        public async Task RemoveAll_Entries_WithType()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(RemoveAll_Entries_WithType)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(RemoveAll_Entries_WithType)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = "client1",
                    Type = "type1",
                }
            ).ToList();

            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            await _store.RemoveAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1", Type = "type1" });

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId })).ToList();

            Assert.Empty(actual);
        }

        [Fact]
        public async Task GetAsync_Does_Not_Return_Expired_Entries()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string key = $"{nameof(GetAsync_Does_Not_Return_Expired_Entries)}-{now:O}";
            string expected = "this is a test";
            var grant = new PersistedGrant { Key = key, Data = expected, ClientId = "client1", SubjectId = "sub1", Type = "type1", Expiration = now.AddSeconds(1) };
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
                    Expiration = now.AddSeconds(2),
                    ClientId = "client1",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId })).ToList();

            Assert.NotNull(actual);
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = $"client{x}",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1" })).ToList();

            Assert.NotNull(actual);
            actual.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_Type()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_Type)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_Type)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = $"client{x}",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, Type = "type1" })).ToList();

            Assert.NotNull(actual);
            actual.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_And_Type()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_And_Type)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_And_Type)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = $"client{x}",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1", Type = "type1" })).ToList();

            Assert.NotNull(actual);
            actual.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_and_SessionId()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_and_SessionId)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_and_SessionId)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = $"client{x}",
                    SessionId = "session1",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1", SessionId = "session1" })).ToList();

            Assert.NotNull(actual);
            actual.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_and_SessionId_and_Type()
        {
            var now = DateTime.Now;
            _clock.Setup(x => x.UtcNow).Returns(now);
            string subjectId = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_and_SessionId_and_Type)}-subjectId";
            var expected = Enumerable.Range(0, 5).Select(x =>
                new PersistedGrant
                {
                    Key = $"{nameof(GetAllAsync_Retrieves_All_Grants_For_SubjectId_and_ClientId_and_SessionId_and_Type)}-{now:O}-{x}",
                    SubjectId = subjectId,
                    Expiration = now.AddSeconds(2),
                    ClientId = $"client{x}",
                    SessionId = "session1",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId, ClientId = "client1", SessionId = "session1", Type = "type1" })).ToList();

            Assert.NotNull(actual);
            actual.Should().HaveCount(1);
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
                    Expiration = now.AddSeconds(-1),
                    ClientId = "client1",
                    Type = "type1",
                }
            ).ToList();
            Task.WaitAll(expected.Select(x => _store.StoreAsync(x)).ToArray());

            var actual = (await _store.GetAllAsync(new IdentityServer4.Stores.PersistedGrantFilter { SubjectId = subjectId })).ToList();

            Assert.NotNull(actual);
            Assert.Empty(actual);
        }
    }
}
