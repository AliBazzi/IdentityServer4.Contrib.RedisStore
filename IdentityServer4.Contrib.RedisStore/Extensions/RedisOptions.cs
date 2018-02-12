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

        internal IConnectionMultiplexer Multiplexer { get; private set; }

        internal void Connect()
        {
            if (Multiplexer is null)
            {
                if (string.IsNullOrEmpty(this.RedisConnectionString))
                    this.Multiplexer = ConnectionMultiplexer.Connect(this.ConfigurationOptions);
                else
                    this.Multiplexer = ConnectionMultiplexer.Connect(this.RedisConnectionString);
            }
        }
    }

    /// <summary>
    /// Represents Redis Configuration store options.
    /// </summary>
    public class RedisConfigurationStoreOptions : RedisOptions
    {

    }

    /// <summary>
    /// Represents Redis Cache options.
    /// </summary>
    public class RedisCacheOptions : RedisOptions
    {

    }
}
