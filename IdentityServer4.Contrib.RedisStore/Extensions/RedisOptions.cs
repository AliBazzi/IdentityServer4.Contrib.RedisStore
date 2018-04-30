﻿using System;
using StackExchange.Redis;

namespace IdentityServer4.Contrib.RedisStore
{
    /// <summary>
    ///     Represents Redis general options.
    /// </summary>
    public abstract class RedisOptions
    {
        /// <summary>
        /// Configuration options objects for StackExchange.Redis Library.
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// Connection String for connecting to Redis Instance.
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        /// The specific Db number to connect to, default is -1.
        /// </summary>
        public int Db { get; set; } = -1;

        private string _keyPrefix = string.Empty;

        /// <summary>
        /// The Prefix to add to each key stored on Redis Cache, default is Empty.
        /// </summary>
        public string KeyPrefix
        {
            get => string.IsNullOrEmpty(_keyPrefix) ? _keyPrefix : $"{_keyPrefix}:";
            set => _keyPrefix = value;
        }

        private Lazy<IConnectionMultiplexer> _multiplexer =>
            GetConnectionMultiplexer(RedisConnectionString, ConfigurationOptions);

        private static Lazy<IConnectionMultiplexer> GetConnectionMultiplexer(string connectionString,
            ConfigurationOptions options)
        {
            return new Lazy<IConnectionMultiplexer>(() =>
                string.IsNullOrEmpty(connectionString)
                    ? ConnectionMultiplexer.Connect(options)
                    : ConnectionMultiplexer.Connect(connectionString));
        }

        internal IConnectionMultiplexer Multiplexer => _multiplexer.Value;
    }

    /// <summary>
    /// Represents Redis Operational store options.
    /// </summary>
    public class RedisOperationalStoreOptions : RedisOptions
    {
    }

    /// <summary>
    /// Represents Redis Cache options.
    /// </summary>
    public class RedisCacheOptions : RedisOptions
    {
    }
}