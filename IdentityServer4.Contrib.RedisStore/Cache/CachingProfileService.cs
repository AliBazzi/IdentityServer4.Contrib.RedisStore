using System.Threading.Tasks;
using IdentityServer4.Contrib.RedisStore;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using IdentityServer4.Extensions;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Caching decorator for IProfileService
    /// </summary>
    /// <seealso cref="IdentityServer4.Services.IProfileService" />
    public class CachingProfileService<TProfileService> : IProfileService
    where TProfileService : class, IProfileService
    {
        private readonly TProfileService inner;

        private readonly ICache<IsActiveContextCacheEntry> cache;

        private readonly ProfileServiceCachingOptions<TProfileService> options;

        private readonly ILogger<CachingProfileService<TProfileService>> logger;

        public CachingProfileService(TProfileService inner, ICache<IsActiveContextCacheEntry> cache, ProfileServiceCachingOptions<TProfileService> options, ILogger<CachingProfileService<TProfileService>> logger)
        {
            this.inner = inner;
            this.logger = logger;
            this.cache = cache;
            this.options = options;
        }

        /// <summary>
        /// This method is called whenever claims about the user are requested (e.g. during token creation or via the userinfo endpoint)
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            await this.inner.GetProfileDataAsync(context);
        }

        /// <summary>
        /// This method gets called whenever identity server needs to determine if the user is valid or active (e.g. if the user's account has been deactivated since they logged in).
        /// (e.g. during token issuance or validation).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task IsActiveAsync(IsActiveContext context)
        {
            var key = $"{options.KeyPrefix}{options.KeySelector(context)}";

            if (options.ShouldCache(context))
            {
                var entry = await cache.GetAsync(key, options.Expiration,
                              async () =>
                              {
                                  await inner.IsActiveAsync(context);
                                  return new IsActiveContextCacheEntry { IsActive = context.IsActive };
                              },
                              logger);

                context.IsActive = entry.IsActive;
            }
            else
            {
                await inner.IsActiveAsync(context);
            }
        }
    }

    /// <summary>
    /// Represents cache entry for IsActiveContext
    /// </summary>
    public class IsActiveContextCacheEntry
    {
        public bool IsActive { get; set; }
    }
}