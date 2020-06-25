using FluentAssertions;
using IdentityModel.Client;
using IdentityServer4.Contrib.RedisStore.Tests.Cache;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer4.Contrib.RedisStore.Tests
{
    public class CachingProfileServiceTests
    {
        private FakeLogger<FakeCache<IsActiveContextCacheEntry>> logger;

        private TestServer CreateTestServer(bool shouldCache)
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddIdentityServer()
                    .AddDeveloperSigningCredential(persistKey: false)
                    .AddInMemoryApiScopes(new List<ApiScope>
                    {
                        new ApiScope("api1")
                    })
                    .AddInMemoryApiResources(new List<ApiResource>
                    {
                        new ApiResource("api1")
                        {
                            ApiSecrets = { new Secret("secret".Sha256())},
                            Scopes = { "api1" }
                        }
                    })
                    .AddInMemoryClients(new List<Client>
                    {
                        new Client
                        {
                            ClientId = "client1",
                            ClientSecrets = { new Secret("secret".Sha256()) },
                            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                            AccessTokenType = AccessTokenType.Reference,
                            AllowedScopes = { "api1" }
                        }
                    })
                    .AddFakeLogger<FakeCache<IsActiveContextCacheEntry>>()
                    .AddFakeMemeoryCaching()
                    .AddResourceOwnerValidator<FakeResourceOwnerPasswordValidator>()
                    .AddProfileService<FakeProfileService>()
                    .AddProfileServiceCache<FakeProfileService>(option =>
                    {
                        option.ShouldCache = context => shouldCache;
                    });
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                    logger = app.ApplicationServices.GetService<FakeLogger<FakeCache<IsActiveContextCacheEntry>>>();
                }));
        }

        [Fact]
        public async Task Test()
        {
            var server = CreateTestServer(shouldCache: true);

            var httpHandler = server.CreateHandler();

            var discoveryClient = new HttpClient(httpHandler);
            discoveryClient.BaseAddress = new Uri("https://idp");
            var docs = await discoveryClient.GetDiscoveryDocumentAsync();

            var client = new HttpClient(httpHandler);
            var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = docs.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Scope = "api1",
                UserName = "test",
                Password = "test"
            });

            var introspection = new HttpClient(httpHandler);
            var introspectionResponse = await introspection.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = docs.IntrospectionEndpoint,
                Token = tokenResponse.AccessToken,
                ClientId = "api1",
                ClientSecret = "secret"
            });
            foreach (var _ in Enumerable.Range(1, 10))
            {
                var result = await introspection.IntrospectTokenAsync(new TokenIntrospectionRequest
                {
                    Address = docs.IntrospectionEndpoint,
                    Token = tokenResponse.AccessToken,
                    ClientId = "api1",
                    ClientSecret = "secret"
                });
                result.IsActive.Should().BeTrue();
            }
            logger.AccessCount["Cache hit for 1"].Should().Equals(10);
        }

        [Fact]
        public async Task Test2()
        {
            var server = CreateTestServer(shouldCache: false);

            var httpHandler = server.CreateHandler();

            var discoveryClient = new HttpClient(httpHandler);
            discoveryClient.BaseAddress = new Uri("https://idp");
            var docs = await discoveryClient.GetDiscoveryDocumentAsync();

            var client = new HttpClient(httpHandler);
            var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = docs.TokenEndpoint,
                ClientId = "client1",
                ClientSecret = "secret",
                Scope = "api1",
                UserName = "test",
                Password = "test"
            });

            var introspection = new HttpClient(httpHandler);
            var introspectionResponse = await introspection.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = docs.IntrospectionEndpoint,
                Token = tokenResponse.AccessToken,
                ClientId = "api1",
                ClientSecret = "secret"
            });
            foreach (var _ in Enumerable.Range(1, 10))
            {
                var result = await introspection.IntrospectTokenAsync(new TokenIntrospectionRequest
                {
                    Address = docs.IntrospectionEndpoint,
                    Token = tokenResponse.AccessToken,
                    ClientId = "api1",
                    ClientSecret = "secret"
                });
                result.IsActive.Should().BeTrue();
            }
            logger.AccessCount.Should().BeEmpty();
        }
    }
}
