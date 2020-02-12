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
                throw new ArgumentNullException(nameof(multiplexer));
            _options = multiplexer.RedisOptions;
            _database = multiplexer.Database;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock;
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

                    var ttlOfClientSet = _database.KeyTimeToLiveAsync(setKeyforClient);
                    var ttlOfSubjectSet = _database.KeyTimeToLiveAsync(setKeyforSubject);

                    await Task.WhenAll(ttlOfSubjectSet, ttlOfClientSet);

                    var transaction = _database.CreateTransaction();
                    transaction.StringSetAsync(grantKey, data, expiresIn);
                    transaction.SetAddAsync(setKeyforSubject, grantKey);
                    transaction.SetAddAsync(setKeyforClient, grantKey);
                    transaction.SetAddAsync(setKey, grantKey);
                    if ((ttlOfSubjectSet.Result ?? TimeSpan.Zero) <= expiresIn)
                        transaction.KeyExpireAsync(setKeyforSubject, expiresIn);
                    if ((ttlOfClientSet.Result ?? TimeSpan.Zero) <= expiresIn)
                        transaction.KeyExpireAsync(setKeyforClient, expiresIn);
                    transaction.KeyExpireAsync(setKey, expiresIn);
                    await transaction.ExecuteAsync();
                }
                else
                {
                    await _database.StringSetAsync(grantKey, data, expiresIn);
                }
                _logger.LogDebug("grant for subject {subjectId}, clientId {clientId}, grantType {grantType} persisted successfully", grant.SubjectId, grant.ClientId, grant.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception storing persisted grant to Redis database for subject {subjectId}, clientId {clientId}, grantType {grantType}", grant.SubjectId, grant.ClientId, grant.Type);
                throw;
            }
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var data = await _database.StringGetAsync(GetKey(key));
            _logger.LogDebug("{key} found in database: {hasValue}", key, data.HasValue);
            return data.HasValue ? ConvertFromJson(data) : null;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var setKey = GetSetKey(subjectId);
            var (grants, keysToDelete) = await GetGrants(setKey);
            if (keysToDelete.Any())
                await _database.SetRemoveAsync(setKey, keysToDelete.ToArray());
            _logger.LogDebug("{grantsCount} persisted grants found for {subjectId}", grants.Count(), subjectId);
            return grants.Where(_ => _.HasValue).Select(_ => ConvertFromJson(_));
        }

        private async Task<(IEnumerable<RedisValue> grants, IEnumerable<RedisValue> keysToDelete)> GetGrants(string setKey)
        {
            var grantsKeys = await _database.SetMembersAsync(setKey);
            if (!grantsKeys.Any())
                return (Enumerable.Empty<RedisValue>(), Enumerable.Empty<RedisValue>());
            var grants = await _database.StringGetAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).ToArray());
            var keysToDelete = grantsKeys.Zip(grants, (key, value) => new KeyValuePair<RedisValue, RedisValue>(key, value))
                                         .Where(_ => !_.Value.HasValue).Select(_ => _.Key);
            return (grants, keysToDelete);
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var grant = await GetAsync(key);
                if (grant == null)
                {
                    _logger.LogDebug("no {key} persisted grant found in database", key);
                    return;
                }
                var grantKey = GetKey(key);
                _logger.LogDebug("removing {key} persisted grant from database", key);
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId), grantKey);
                transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId, grant.Type), grantKey);
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception removing {key} persisted grant from database", key);
                throw;
            }

        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId);
                var grantsKeys = await _database.SetMembersAsync(setKey);
                _logger.LogDebug("removing {grantsCount} persisted grants from database for subject {subjectId}, clientId {clientId}", grantsKeys.Count(), subjectId, clientId);
                if (!grantsKeys.Any()) return;
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception removing persisted grants from database for subject {subjectId}, clientId {clientId}", subjectId, clientId);
                throw;
            }
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            try
            {
                var setKey = GetSetKey(subjectId, clientId, type);
                var grantsKeys = await _database.SetMembersAsync(setKey);
                _logger.LogDebug("removing {grantKeysCount} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}", grantsKeys.Count(), subjectId, clientId, type);
                if (!grantsKeys.Any()) return;
                var transaction = _database.CreateTransaction();
                transaction.KeyDeleteAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
                transaction.SetRemoveAsync(GetSetKey(subjectId, clientId), grantsKeys);
                transaction.SetRemoveAsync(GetSetKey(subjectId), grantsKeys);
                await transaction.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "exception removing persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {type}", subjectId, clientId, type);
                throw;
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