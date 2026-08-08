using System.Collections.Concurrent;

namespace SeinServices.Api.Services.FaultMon
{
    /// <summary>
    /// SignalR 접속 상태를 관리합니다.
    /// </summary>
    public class FaultMonConnectionTracker
    {
        private readonly ConcurrentDictionary<string, byte> _connections = new();

        public int ActiveConnectionCount => _connections.Count;

        public int Add(string connectionId)
        {
            _connections.TryAdd(connectionId, 0);
            return ActiveConnectionCount;
        }

        public int Remove(string connectionId)
        {
            _connections.TryRemove(connectionId, out _);
            return ActiveConnectionCount;
        }
    }
}
