using System;
using StackExchange.Redis;

namespace IdentityServer4.Contrib.RedisStore
{
    /// <summary>
    /// Represents Redis general options.
    /// </summary>
    public abstract class RedisOptions
    {
        /// <summary>
        ///Configuration options objects for StackExchange.Redis Library.
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// Connection String for connecting to Redis Instance.
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        ///The specific Db number to connect to, default is -1.
        /// </summary>
        public int Db { get; set; } = -1;

        private string _keyPrefix = string.Empty;

        /// <summary>
        /// The Prefix to add to each key stored on Redis Cache, default is Empty.
        /// </summary>
        public string KeyPrefix
        {
            get
            {
                return string.IsNullOrEmpty(this._keyPrefix) ? this._keyPrefix : $"{_keyPrefix}:";
            }
            set
            {
                this._keyPrefix = value;
            }
        }

        internal RedisOptions()
        {
            this.multiplexer = GetConnectionMultiplexer();
        }

        private Lazy<IConnectionMultiplexer> GetConnectionMultiplexer()
        {
            return new Lazy<IConnectionMultiplexer>(
                () => string.IsNullOrEmpty(this.RedisConnectionString)
                    ? ConnectionMultiplexer.Connect(this.ConfigurationOptions)
                    : ConnectionMultiplexer.Connect(this.RedisConnectionString));
        }

        private Lazy<IConnectionMultiplexer> multiplexer = null;

        internal IConnectionMultiplexer Multiplexer => this.multiplexer.Value;
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
