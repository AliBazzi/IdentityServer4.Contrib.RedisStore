using IdentityServer4.Contrib.RedisStore.Tests;
using IdentityServer4.Contrib.RedisStore.Tests.Cache;
using IdentityServer4.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class TestingExtensions
    {

        public static IIdentityServerBuilder AddFakeMemeoryCaching(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            builder.Services.AddScoped(typeof(ICache<>), typeof(FakeCache<>));
            return builder;
        }

        public static IIdentityServerBuilder AddFakeLogger<T>(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton(new FakeLogger<T>());
            return builder;
        }
    }
}
