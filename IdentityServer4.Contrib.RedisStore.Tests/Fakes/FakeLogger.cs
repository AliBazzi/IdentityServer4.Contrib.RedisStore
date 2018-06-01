using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Contrib.RedisStore.Tests
{
    public class FakeLogger<T> : ILogger<T>
    {

        private Dictionary<string, int> accessCount = new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> AccessCount => accessCount;

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (accessCount.ContainsKey(state.ToString()))
            { accessCount[state.ToString()] += 1; }
            else
            { accessCount[state.ToString()] = 1; }
        }
    }
}
