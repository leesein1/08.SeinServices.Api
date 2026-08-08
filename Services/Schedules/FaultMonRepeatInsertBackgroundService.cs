using Microsoft.AspNetCore.SignalR;
using SeinServices.Api.Hubs.FaultMon;
using SeinServices.Api.Services.FaultMon;

namespace SeinServices.Api.Services.Schedules
{
    /// <summary>
    /// FaultMon 접속자 기준 반복 프로시저 실행 서비스입니다.
    /// </summary>
    public class FaultMonRepeatInsertBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<FaultMonHub> _hubContext;
        private readonly FaultMonConnectionTracker _connectionTracker;
        private readonly ILogger<FaultMonRepeatInsertBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public FaultMonRepeatInsertBackgroundService(
            IServiceScopeFactory scopeFactory,
            IHubContext<FaultMonHub> hubContext,
            FaultMonConnectionTracker connectionTracker,
            IConfiguration configuration,
            ILogger<FaultMonRepeatInsertBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _connectionTracker = connectionTracker;
            _logger = logger;

            var seconds = configuration.GetValue("FaultMon:RepeatInsertIntervalSeconds", 5);
            _interval = TimeSpan.FromSeconds(Math.Max(1, seconds));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (_connectionTracker.ActiveConnectionCount <= 0)
                {
                    continue;
                }

                await ExecuteRepeatInsert(stoppingToken);
            }
        }

        private async Task ExecuteRepeatInsert(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<FaultMonService>();
                var affectedRows = service.ExecuteScheduleRepeatInsert();

                var payload = new
                {
                    occurredAt = DateTimeOffset.Now,
                    activeConnections = _connectionTracker.ActiveConnectionCount,
                    affectedRows
                };

                await _hubContext.Clients.All.SendAsync("Signal_FLTLIST", payload, stoppingToken);
                await _hubContext.Clients.All.SendAsync("FaultMonScheduleTick", payload, stoppingToken);

                _logger.LogInformation(
                    "FaultMon repeat insert completed. Active={Active}, AffectedRows={AffectedRows}",
                    payload.activeConnections,
                    affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FaultMon repeat insert failed.");

                await _hubContext.Clients.All.SendAsync(
                    "FaultMonScheduleError",
                    new
                    {
                        occurredAt = DateTimeOffset.Now,
                        message = "PROC_SCH_REPEAT_INSERT failed."
                    },
                    stoppingToken);
            }
        }
    }
}
