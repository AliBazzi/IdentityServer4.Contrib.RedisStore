

# IdentityServer4.Contrib.RedisStore

IdentityServer4.Contrib.RedisStore is a persistence layer using [Redis](https://redis.io) DB for operational data and for caching capability for Identity Server 4. Specifically, this store provides implementation for [IPersistedGrantStore](http://docs.identityserver.io/en/release/topics/deployment.html#operational-data) and [ICache<T>](http://docs.identityserver.io/en/release/topics/startup.html#caching).


## How to use

You need to install the [nuget package](https://www.nuget.org/packages/IdentityServer4.Contrib.RedisStore)

then you can inject the stores in the Identity Server 4 Configuration at startup:

```csharp
public void ConfigureServices(IServiceCollection services)
        {
            const string connectionString = @"---redis store connection string---";

            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                ...
                .AddOperationalStore(connectionString, db: 0)
                .AddRedisCaching(connectionString, db: 1);
        }
```

Or by passing ConfigurationOptions instance, which contains the configuration of Redis store:

```csharp
public void ConfigureServices(IServiceCollection services)
        {
            var operationalStoreOptions = new ConfigurationOptions {  /* ... */ };
            var cacheOptions = new ConfigurationOptions {  /* ... */ };
            services.AddMvc();

            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                ...
                .AddOperationalStore(operationalStoreOptions)
                .AddRedisCaching(cacheOptions);
        }
```

don't forget to register the caching for specific configuration store you like to apply the caching on, like the following:

```csharp
public void ConfigureServices(IServiceCollection services)
        {
            var operationalStoreOptions = new ConfigurationOptions {  /* ... */ };
            var cacheOptions = new ConfigurationOptions {  /* ... */ };

            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                .AddConfigurationStore(options =>
            	{
                	options.ConfigureDbContext = ... ;
            	})
                ...
                .AddRedisCaching(cacheOptions)
                .AddClientStoreCache<IdentityServer4.EntityFramework.Stores.ClientStore>()
            	.AddResourceStoreCache<IdentityServer4.EntityFramework.Stores.ResourceStore>();
        }

```

In this previous snippet, registration of caching capability are added for Client Store and Resource Store, and it's registered it for Entity Framework stores in this case, but if you have your own Stores you should register them here in order to allow the caching for these specific stores.  

###### Note

operational store and caching are not related, you can use them separately or combined.

## the solution approach

the solution was approached based on how the [SQL Store](https://github.com/IdentityServer/IdentityServer4.EntityFramework) storing the operational data, but the concept of Redis as a NoSQL db is totally different than relational db concepts, all the operational data stores implement the following [IPersistedGrantStore](https://github.com/IdentityServer/IdentityServer4/blob/dev/src/IdentityServer4/Stores/IPersistedGrantStore.cs) interface:

```csharp
    public interface IPersistedGrantStore
    {
        Task StoreAsync(PersistedGrant grant);

        Task<PersistedGrant> GetAsync(string key);

        Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId);

        Task RemoveAsync(string key);

        Task RemoveAllAsync(string subjectId, string clientId);

        Task RemoveAllAsync(string subjectId, string clientId, string type);
    }
```

with the IPersistedGrantStore contract, we notice that the GetAllAsync(subjectId), RemoveAllAsync(subjectId,clientId) and RemoveAllAsync(subjectId,clientId,type) defines a contract to read based on subject id and remove all the grants in the store based on subject, client ids and type of the grant.

this brings trouble to Redis store since redis as a reliable dictionary is not designed for relational queries, so the trick is to store multiple key entries for the same grant, and the keys can be reached using key, subject, client ids and type.

so the StoreAsync operation stores the following entries in Redis:

1. Key -> RedisStruct: stored as key string value pairs, used to retrieve the grant based on the key, if the grant exists or not expired.

1. Key(SubjectId) -> Key* : stored in a redis Set, used on the GetAllAsync, to retrieve all the grant related to a given subject id.

1. Key(SubjectId:ClientId) -> Key* : stored in a redis set, used to retrieve all the keys that are related to a subject and client ids, to remove them while calling RemoveAllAsync.

1. Key(SubjectId:ClientId:type) -> Key* : stored in a redis set, used to retrieve all the keys that are related to a subject, client ids and type of the grant, to remove them while calling RemoveAllAsync.

for more information on data structures used to store the grant please refer to [Redis data types documentation](https://redis.io/topics/data-types)

since Redis has a [key Expiration](https://redis.io/commands/expire) feature based on a defined date time or time span, and to not implement a logic similar to SQL store implementation for [cleaning up the store](http://docs.identityserver.io/en/release/quickstarts/8_entity_framework.html) periodically from dangling grants, the store uses the key expiration of redis while storing based on the following criteria:

1. for Key of the grant, the expiration is straight forward, it's set on the StringSet Redis operation as defined by identity server on the grant object.

1. for Key(SubjectId), Key(SubjectId:ClientId) and Key(SubjectId:clientId:type) the expiration is not set, since the same and only store type is persisting the grants regardless of their type, not like the identity server 3, where it has multiple store for each grant type.

## Feedback

feedbacks are always welcomed, please open an issue for any problem or bug found, and the suggestions are also welcomed.


