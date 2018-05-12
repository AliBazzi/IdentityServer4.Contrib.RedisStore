using IdentityServer4.Services;
using IdentityServer4.Stores.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RedisStore.Cache
{
    /// <summary>
    /// Redis based implementation for ICache<typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisCache<T> : ICache<T> where T : class
    {
        private readonly IDatabase database;

        private readonly RedisCacheOptions options;

        private readonly ILogger<RedisCache<T>> logger;

        public RedisCache(RedisMultiplexer<RedisCacheOptions> multiplexer, ILogger<RedisCache<T>> logger)
        {
            if (multiplexer is null)
                throw new ArgumentNullException(nameof(multiplexer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.options = multiplexer.RedisOptions;
            this.database = multiplexer.Database;
        }

        private string GetKey(string key) => $"{this.options.KeyPrefix}{typeof(T).FullName}:{key}";

        public async Task<T> GetAsync(string key)
        {
            var cacheKey = GetKey(key);
            var item = await this.database.StringGetAsync(cacheKey).ConfigureAwait(false);
            if (item.HasValue)
            {
                logger.LogDebug($"retrieved {typeof(T).FullName} with Key: {key} from Redis Cache successfully.");
                return Deserialize(item);
            }
            else
            {
                logger.LogDebug($"missed {typeof(T).FullName} with Key: {key} from Redis Cache.");
                return default(T);
            }
        }

        public async Task SetAsync(string key, T item, TimeSpan expiration)
        {
            var cacheKey = GetKey(key);
            await this.database.StringSetAsync(cacheKey, Serialize(item), expiration).ConfigureAwait(false);
            logger.LogDebug($"persisted {typeof(T).FullName} with Key: {key} in Redis Cache successfully.");
        }

        #region Json
        private JsonSerializerSettings SerializerSettings
        {
            get
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new ClaimConverter());
                return settings;
            }
        }

        private T Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, this.SerializerSettings);
        }

        private string Serialize(T item)
        {
            return JsonConvert.SerializeObject(item, this.SerializerSettings);
        }
        #endregion
    }
}
