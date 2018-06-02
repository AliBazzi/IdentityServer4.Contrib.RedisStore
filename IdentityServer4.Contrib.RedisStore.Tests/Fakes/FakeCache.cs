using IdentityServer4.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RedisStore.Tests.Cache
{
    public class FakeCache<T> : ICache<T> where T : class
    {
        private readonly IMemoryCache cache;

        private readonly ILogger<FakeCache<T>> logger;

        public FakeCache(IMemoryCache memoryCache, FakeLogger<FakeCache<T>> logger)
        {
            cache = memoryCache;
            this.logger = logger;
        }

        public Task<T> GetAsync(string key)
        {
            var result = cache.Get(key);

            if (result == null)
                logger.LogDebug($"Cache miss for {key}");
            else
                logger.LogDebug($"Cache hit for {key}");

            return Task.FromResult((T)result);
        }

        public Task SetAsync(string key, T item, TimeSpan expiration)
        {
            cache.Set(key, item, expiration);
            return Task.CompletedTask;
        }
    }
}
