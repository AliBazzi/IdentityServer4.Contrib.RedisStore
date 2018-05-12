using Microsoft.Extensions.Configuration;

namespace IdentityServer4.Contrib.RedisStore.Tests
{
    public static class ConfigurationUtils
    {
	    public static IConfiguration GetConfiguration()
	    {
		    var config = new ConfigurationBuilder()
			    .AddJsonFile("appsettings.json");

		    return config.Build();
	    }
    }
}
