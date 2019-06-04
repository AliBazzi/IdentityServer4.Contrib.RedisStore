using StackExchange.Redis;
using StackExchange.Redis.Profiling;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RedisStore.Tests.Fakes
{
    internal class FakeConnectionMultiplexer : IConnectionMultiplexer
    {
        public string ClientName { get; set; }

        public string Configuration { get; set; }

        public int TimeoutMilliseconds { get; set; }

        public long OperationCount { get; set; }

        public bool PreserveAsyncOrder { get; set; }

        public bool IsConnected { get; set; }

        public bool IsConnecting { get; set; }

        public bool IncludeDetailInExceptions { get; set; }
        public int StormLogThreshold { get; set; }

        public event EventHandler<RedisErrorEventArgs> ErrorMessage;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;
        public event EventHandler<InternalErrorEventArgs> InternalError;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored;
        public event EventHandler<EndPointEventArgs> ConfigurationChanged;
        public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast;
        public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved;

        public void Close(bool allowCommandsToComplete = true) { }
        public Task CloseAsync(bool allowCommandsToComplete = true) => Task.CompletedTask;
        public bool Configure(TextWriter log = null) => true;
        public Task<bool> ConfigureAsync(TextWriter log = null) => Task.FromResult(true);
        public void Dispose() { }
        public void ExportConfiguration(Stream destination, ExportOptions options = (ExportOptions)(-1)) { }
        public ServerCounters GetCounters() => new ServerCounters(null);
        public IDatabase GetDatabase(int db = -1, object asyncState = null) => null;
        public EndPoint[] GetEndPoints(bool configuredOnly = false) => new EndPoint[0];
        public int GetHashSlot(RedisKey key) => -1;
        public IServer GetServer(string host, int port, object asyncState = null) => null;
        public IServer GetServer(string hostAndPort, object asyncState = null) => null;
        public IServer GetServer(IPAddress host, int port) => null;
        public IServer GetServer(EndPoint endpoint, object asyncState = null) => null;
        public string GetStatus() => string.Empty;
        public void GetStatus(TextWriter log) { }
        public string GetStormLog() => string.Empty;
        public ISubscriber GetSubscriber(object asyncState = null) => null;
        public int HashSlot(RedisKey key) => -1;
        public long PublishReconfigure(CommandFlags flags = CommandFlags.None) => -1;
        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None) => Task.FromResult(-1L);
        public void RegisterProfiler(Func<ProfilingSession> profilingSessionProvider) { }
        public void ResetStormLog() { }
        public void Wait(Task task) { }
        public T Wait<T>(Task<T> task) => default(T);
        public void WaitAll(params Task[] tasks) { }
    }
}
