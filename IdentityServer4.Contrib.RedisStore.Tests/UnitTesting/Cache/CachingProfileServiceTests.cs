using IdentityServer4.Models;
using IdentityServer4.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using FakeItEasy;

namespace IdentityServer4.Contrib.RedisStore.Tests.Cache
{
    public class CachingProfileServiceTests
    {
        private readonly FakeProfileService inner;
        private readonly FakeCache<IsActiveContextCacheEntry> cache;
        private readonly FakeLogger<FakeCache<IsActiveContextCacheEntry>> logger;
        private readonly CachingProfileService<FakeProfileService> profileServiceCache;
        private readonly IMemoryCache memoryCache;

        public CachingProfileServiceTests()
        {
            inner = new FakeProfileService();
            memoryCache = new MemoryCache(new MemoryCacheOptions());
            logger = new FakeLogger<FakeCache<IsActiveContextCacheEntry>>();
            cache = new FakeCache<IsActiveContextCacheEntry>(memoryCache, logger);
            profileServiceCache = new CachingProfileService<FakeProfileService>(inner, cache, new ProfileServiceCachingOptions<FakeProfileService>(), Mock.Of<ILogger<CachingProfileService<FakeProfileService>>>());
        }

        [Fact]
        public async Task AssertHitingDataStoreAtLeastOnce()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "1") }));
            var context = new IsActiveContext(principal, new Client(), "test");
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            context.IsActive.Should().BeTrue();
            logger.AccessCount["Cache hit for 1"].Should().Equals(2);
        }

        [Fact]
        public async Task AssertIsInactive()
        {
            inner.IsActive = cxt => cxt.IsActive = false;
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "1") }));
            var context = new IsActiveContext(principal, new Client(), "test");
            await profileServiceCache.IsActiveAsync(context);
            context.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task AssertExpiryOfCacheEntry()
        {
            var profileServiceCache = new CachingProfileService<FakeProfileService>(inner, cache, new ProfileServiceCachingOptions<FakeProfileService>() { Expiration = TimeSpan.FromSeconds(1) }, Mock.Of<ILogger<CachingProfileService<FakeProfileService>>>());
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim("sub", "1") }));
            var context = new IsActiveContext(principal, new Client(), "test");
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            Thread.Sleep(1000);
            await profileServiceCache.IsActiveAsync(context);
            await profileServiceCache.IsActiveAsync(context);
            context.IsActive.Should().BeTrue();
            logger.AccessCount["Cache hit for 1"].Should().Equals(2);
        }
    }
}
