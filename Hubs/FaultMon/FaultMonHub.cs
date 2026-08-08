using Microsoft.AspNetCore.SignalR;
using SeinServices.Api.Services.FaultMon;

namespace SeinServices.Api.Hubs.FaultMon
{
    /// <summary>
    /// FaultMon 실시간 관제 Hub입니다.
    /// </summary>
    public class FaultMonHub : Hub
    {
        private readonly FaultMonConnectionTracker _connectionTracker;
        private readonly ILogger<FaultMonHub> _logger;

        public FaultMonHub(
            FaultMonConnectionTracker connectionTracker,
            ILogger<FaultMonHub> logger)
        {
            _connectionTracker = connectionTracker;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var count = _connectionTracker.Add(Context.ConnectionId);
            _logger.LogInformation(
                "FaultMon SignalR connected. ConnectionId={ConnectionId}, Active={Active}",
                Context.ConnectionId,
                count);

            await BroadcastUserCount(count);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var count = _connectionTracker.Remove(Context.ConnectionId);
            _logger.LogInformation(
                exception,
                "FaultMon SignalR disconnected. ConnectionId={ConnectionId}, Active={Active}",
                Context.ConnectionId,
                count);

            await BroadcastUserCount(count);
            await base.OnDisconnectedAsync(exception);
        }

        public Task GetUserCount()
        {
            return Clients.Caller.SendAsync("FaultMonUserCount", _connectionTracker.ActiveConnectionCount);
        }

        private Task BroadcastUserCount(int count)
        {
            return Clients.All.SendAsync("FaultMonUserCount", count);
        }
    }
}
