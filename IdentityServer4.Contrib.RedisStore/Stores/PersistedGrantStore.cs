using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RedisStore.Stores
{
    /// <summary>
    /// Provides the implementation of IPersistedGrantStore for Redis Cache.
    /// </summary>
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly RedisOperationalStoreOptions _options;

        private readonly IDatabase _database;

        private readonly ILogger<PersistedGrantStore> _logger;

        private readonly ISystemClock _clock;

        public PersistedGrantStore(RedisMultiplexer<RedisOperationalStoreOptions> multiplexer, ILogger<PersistedGrantStore> logger, ISystemClock clock)
        {
            if (multiplexer is null)
            {
                throw new ArgumentNullException(nameof(multiplexer));
            }
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _options = multiplexer.RedisOptions;
            _database = multiplexer.Database;
        }

        private string GetKey(string key) => $"{_options.KeyPrefix}{key}";

        private string GetSetKey(string subjectId) => $"{_options.KeyPrefix}{subjectId}";

        private string GetSetKey(string subjectId, string clientId) => $"{_options.KeyPrefix}{subjectId}:{clientId}";

        private string GetSetKey(string subjectId, string clientId, string type) => $"{_options.KeyPrefix}{subjectId}:{clientId}:{type}";

        public async Task StoreAsync(PersistedGrant grant)
        {
            if (grant == null)
                throw new ArgumentNullException(nameof(grant));
            try
            {
                var data = ConvertToJson(grant);
                var grantKey = GetKey(grant.Key);
                var expiresIn = grant.Expiration - _clock.UtcNow;
                if (!string.IsNullOrEmpty(grant.SubjectId))
                {
                    var setKey = GetSetKey(grant.SubjectId, grant.ClientId, grant.Type);
                    var setKeyforSubject = GetSetKey(grant.SubjectId);
                    var setKeyforClient = GetSetKey(grant.SubjectId, grant.ClientId);

                    //get keys to clean, if any
                    var (_, keysToDelete) = await GetGrants(setKeyforSubject).ConfigureAwait(false);

                    var transaction = _database.CreateTransaction();
                    transaction.StringSetAsync(grantKey, data, expiresIn);
                    transaction.SetAddAsync(setKeyforSubject, grantKey);
                    transaction.SetAddAsync(setKeyforClient, grantKey);
                    transaction.SetAddAsync(setKey, grantKey);
                    transaction.KeyExpireAsync(setKey, expiresIn);

                    if (keysToDelete.Any())//cleanup sets while persisting new grant
                    {
                        transaction.SetRemoveAsync(setKey, keysToDelete.ToArray());
                        transaction.SetRemoveAsync(setKeyforSubject, keysToDelete.ToArray());
                        transaction.SetRemoveAsync(setKeyforClient, keysToDelete.ToArray());
                    }
                    await transaction.ExecuteAsync().ConfigureAwait(false);
                }
                else
                {
                    await _database.StringSetAsync(grantKey, data, expiresIn).ConfigureAwait(false);
                }
                _logger.LogDebug($"grant for subject {grant.SubjectId}, clientId {grant.ClientId}, grantType {grant.Type} persisted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"exception storing persisted grant to Redis _database for subject {grant.SubjectId}, clientId {grant.ClientId}, grantType {grant.Type} : {ex.Message}");
            }

        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var data = await _database.StringGetAsync(GetKey(key)).ConfigureAwait(false);
            _logger.LogDebug($"{key} found in _database: {data.HasValue}");
            return data.HasValue ? ConvertFromJson(data) : null;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var setKey = GetSetKey(subjectId);
            var (grants, keysToDelete) = await GetGrants(setKey).ConfigureAwait(false);
            if (keysToDelete.Any())
                await _database.SetRemoveAsync(setKey, keysToDelete.ToArray()).ConfigureAwait(false);
            _logger.LogDebug($"{grants.Count()} persisted grants found for {subjectId}");
            return grants.Where(_ => _.HasValue).Select(_ => ConvertFromJson(_));
        }

        private async Task<(IEnumerable<RedisValue> grants, IEnumerable<RedisValue> keysToDelete)> GetGrants(string setKey)
        {
            var grantsKeys = await _database.SetMembersAsync(setKey).ConfigureAwait(false);
            if (!grantsKeys.Any())
                return (Enumerable.Empty<RedisValue>(), Enumerable.Empty<RedisValue>());
            var grants = await _database.StringGetAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).ToArray()).ConfigureAwait(false);
            var keysToDelete = grantsKeys.Zip(grants, (key, value) => new KeyValuePair<RedisValue, RedisValue>(key, value))
                                         .Where(_ => !_.Value.HasValue).Select(_ => _.Key);
            return (grants, keysToDelete);
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var grant = await GetAsync(key).ConfigureAwait(false);
                if (grant == null)
                {
                    _logger.LogDebug($"no {key} persisted grant found in _database");
                    return;
                }
                var grantKey = GetKey(key);
                _logger.LogDebug($"removing {key} persisted grant from _database");
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId, grant.Type), grantKey);
                await transaction.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"exception removing {key} persisted grant from _database: {ex.Message}");
            }

        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId);
                var grantsKeys = await _database.SetMembersAsync(setKey).ConfigureAwait(false);
                _logger.LogDebug($"removing {grantsKeys.Length} persisted grants from _database for subject {subjectId}, clientId {clientId}");
                if (!grantsKeys.Any()) return;
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
                await transaction.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"removing persisted grants from _database for subject {subjectId}, clientId {clientId}: {ex.Message}");
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId, type);
                var grantsKeys = await _database.SetMembersAsync(setKey).ConfigureAwait(false);
                _logger.LogDebug($"removing {grantsKeys.Length} persisted grants from _database for subject {subjectId}, clientId {clientId}, grantType {type}");
                if (!grantsKeys.Any()) return;
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId, clientId), grantsKeys);
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
                await transaction.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"exception removing persisted grants from _database for subject {subjectId}, clientId {clientId}, grantType {type}: {ex.Message}");
            }
        }

        #region Json
        private static string ConvertToJson(PersistedGrant grant)
        {
            return JsonConvert.SerializeObject(grant);
        }

        private static PersistedGrant ConvertFromJson(string data)
        {
            return JsonConvert.DeserializeObject<PersistedGrant>(data);
        }
        #endregion
    }
}
