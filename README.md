# IdentityServer4.Contrib.RedisStore

IdentityServer4.Contrib.RedisStore is a persistence layer using [Redis](https://redis.io) DB for operational data and for caching capability for Identity Server 4. Specifically, this store provides implementation for [IPersistedGrantStore](http://docs.identityserver.io/en/release/topics/deployment.html#operational-data) and [ICache<T>](http://docs.identityserver.io/en/release/topics/startup.html#caching).

## How to use

You need to install the [nuget package](https://www.nuget.org/packages/IdentityServer4.Contrib.RedisStore)

then you can inject the operational store in the Identity Server 4 Configuration at startup using one of the overloads of `AddOperationalStore`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddIdentityServer()
    ...
    .AddOperationalStore(options =>
    {
        options.RedisConnectionString = "---redis store connection string---";
        options.Db = 1;
    })
    ...
}
```

And for adding caching capability you can use `AddRedisCaching`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddIdentityServer()
    ...
    .AddRedisCaching(options =>
    {
        options.RedisConnectionString = "---redis store connection string---";
        options.KeyPrefix = "prefix";
    })
    ...
}
```

As an alternative, you can pass ConfigurationOptions instance, which contains the configuration of Redis store:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var operationalStoreOptions = new ConfigurationOptions {  /* ... */ };
    var cacheOptions = new ConfigurationOptions {  /* ... */ };

    ...

    services.AddIdentityServer()
    ...
    .AddOperationalStore(options =>
    {
        options.ConfigurationOptions = operationalStoreOptions;
        options.KeyPrefix = "another_prefix";
    })
    .AddRedisCaching(options =>
    {
        options.ConfigurationOptions = cacheOptions;
    })
    ...
}
```

Finally, you have the option of passing an already established connection using `ConnectionMultiplexer` by passing it directly like:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddIdentityServer()
    ...
    .AddOperationalStore(options =>
    {
        options.RedisConnectionMultiplexer = connectionMultiplexer;
    })
    .AddRedisCaching(options =>
    {
        options.RedisConnectionMultiplexer = connectionMultiplexer;
    })
    ...
}
```

don't forget to register the caching for specific configuration store you like to apply the caching on after registering the services, like the following:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddIdentityServer()
    ...
    .AddRedisCaching(options =>
    {
        options.ConfigurationOptions = cacheOptions;
    })
    ...
    .AddClientStoreCache<IdentityServer4.EntityFramework.Stores.ClientStore>()
    .AddResourceStoreCache<IdentityServer4.EntityFramework.Stores.ResourceStore>()
    .AddCorsPolicyCache<IdentityServer4.EntityFramework.Services.CorsPolicyService>()
    .AddProfileServiceCache<MyProfileService>()
    ...
}

```

In this previous snippet, registration of caching capability are added for Client Store, Resource Store and Cors Policy Service, and it's registered for [Entity Framework stores](https://github.com/IdentityServer/IdentityServer4/tree/main/src/EntityFramework.Storage) in this case, but if you have your own Stores you should register them here in order to allow the caching for these specific stores.

> Note: operational store and caching are not related, you can use them separately or combined.

> Note: for `AddProfileServiceCache`, you can configure it with custom key selector, the default implementation is to select `sub` claim value.

## the solution approach

the solution was approached based on how the [SQL Store](https://github.com/IdentityServer/IdentityServer4/tree/main/src/EntityFramework.Storage) storing the operational data, but the concept of Redis as a NoSQL db is totally different than relational db concepts, all the operational data stores implement the following [IPersistedGrantStore](https://github.com/IdentityServer/IdentityServer4/blob/main/src/Storage/src/Stores/IPersistedGrantStore.cs) interface:

```csharp
public interface IPersistedGrantStore
{
    Task StoreAsync(PersistedGrant grant);

    Task<PersistedGrant> GetAsync(string key);

    Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter);

    Task RemoveAsync(string key);

    Task RemoveAllAsync(PersistedGrantFilter filter);
}
```

with the IPersistedGrantStore contract, we notice that the GetAllAsync(filter), RemoveAllAsync(filter) defines a contract to read based on subject id and remove all the grants in the store based on subject, client ids and/or session ids and type of the grant.

this brings trouble to Redis store since redis as a reliable dictionary is not designed for relational queries, so the trick is to store multiple key entries for the same grant, and the keys can be reached using key, subject, client ids, session ids and type.

so the StoreAsync operation stores the following entries in Redis:

1. Key -> RedisStruct: stored as key string value pairs, used to retrieve/remove the grant based on the key, if the grant exists or not expired.

1. Key(SubjectId) -> Key\* : stored in a redis Set, used on retrieve all/remove all, to retrieve all the grant related to a given subject id.

1. Key(SubjectId,ClientId) -> Key\* : stored in a redis set, used to retrieve all/remove all the keys that are related to a subject and client ids.

1. Key(SubjectId,ClientId,type) -> Key\* : stored in a redis set, used to retrieve all/remove all the keys that are related to a subject, client ids and type of the grant.

1. Key(SubjectId,ClientId,SessionId) -> Key\* : stored in a redis set, used to retrieve all/remove all the keys that are related to a subject, client ids, sessions ids.

for more information on data structures used to store the grant please refer to [Redis data types documentation](https://redis.io/topics/data-types)

> Note: PersistedGrantFilter combinations are not all covered by sets persisted in Redis, if the combination used to be not mapping to an already existing set key, then the retrieval for grants will fallback to Key(SubjectId) -> Key, and the evaluation of the filter will happen on client side.

since Redis has a [key Expiration](https://redis.io/commands/expire) feature based on a defined date time or time span, and to not implement a logic similar to SQL store implementation for [cleaning up the store](http://docs.identityserver.io/en/release/quickstarts/8_entity_framework.html) periodically from dangling grants, the store uses the key expiration of Redis while storing entries based on the following criteria:

1. for Key of the grant, the expiration is straight forward, it's set on the StringSet Redis operation as defined by identity server on the grant object.

1. for `Key(SubjectId,ClientId,type)` it's absolute expiry time, but it will be extended every time new entry is added to the set.

1. for `Key(SubjectId)`, `Key(SubjectId,ClientId)` and `Key(SubjectId,ClientId,SessionId)` the expiration is sliding, and it will slide on every entry added to the set, since the same and only store type is persisting the grants regardless of their type, not like the identity server 3, where it has multiple stores for each grant type. and we are setting expiration for Key(SubjectId,clientId,type) since this set for the same grant type, and client, so the keys are consistent here.

> Note: because of the sliding expiration mechanism, if Redis instance memory is full, [eviction](https://redis.io/topics/lru-cache) will take places based on the policy you set.

## Feedback

feedbacks are always welcomed, please open an issue for any problem or bug found, and the suggestions are also welcomed.
