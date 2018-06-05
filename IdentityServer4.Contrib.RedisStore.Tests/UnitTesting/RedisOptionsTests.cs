using Xunit;

namespace IdentityServer4.Contrib.RedisStore.Tests.UnitTesting
{
    public class RedisOptionsTests
    {
        [Fact]
        public void RedisOptions_Multiplexer_Is_Only_Created_Once()
        {
            string connectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"];
            var options = new TestRedisOptions {RedisConnectionString = connectionString};

            var multiplexer = options.Multiplexer;
            var multiplexer2 = options.Multiplexer;

            Assert.Same(multiplexer, multiplexer2);
        }

	    private class TestRedisOptions : RedisOptions
	    {

	    }
    }
}
