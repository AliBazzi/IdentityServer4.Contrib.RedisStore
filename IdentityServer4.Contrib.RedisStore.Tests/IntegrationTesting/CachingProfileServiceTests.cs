using FluentAssertions;
using IdentityModel.Client;
using IdentityServer4.Contrib.RedisStore.Tests.Cache;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServer4.Contrib.RedisStore.Tests
{
    public class CachingProfileServiceTests
    {
        private FakeLogger<FakeCache<IsActiveContextCacheEntry>> logger;

        private TestServer CreateTestServer()
        {
            return new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddIdentityServer()
                    .AddDeveloperSigningCredential(persistKey: false)
                    .AddInMemoryApiResources(new List<ApiResource>
                    {
                        new ApiResource("api1")
                        {
                            ApiSecrets = { new Secret("secret".Sha256())}
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
                    .AddProfileServiceCache<FakeProfileService>();
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
            var server = CreateTestServer();

            var httpHandler = server.CreateHandler();

            var discoveryClient = new DiscoveryClient("https://idp", httpHandler);
            var doc = await discoveryClient.GetAsync();

            var client = new TokenClient(doc.TokenEndpoint, "client1", "secret", httpHandler);
            var tokenResponse = await client.RequestResourceOwnerPasswordAsync(userName: "test", password: "test");

            var introspection = new IntrospectionClient(doc.IntrospectionEndpoint, "api1", "secret", httpHandler);

            foreach (var _ in Enumerable.Range(1, 10))
                (await introspection.SendAsync(new IntrospectionRequest { Token = tokenResponse.AccessToken })).IsActive.Should().BeTrue();

            logger.AccessCount["Cache hit for 1"].Should().Equals(10);
        }

    }
}
