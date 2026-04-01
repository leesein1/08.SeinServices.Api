using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Services.Schedules
{
    /// <summary>
    /// RecruitSyncBackgroundService 관련 기능을 제공합니다.
    /// </summary>
    public class RecruitSyncBackgroundService : BackgroundService
    {
        private static readonly TimeZoneInfo KoreaTimeZone = ResolveKoreaTimeZone();

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecruitSyncBackgroundService> _logger;

        public RecruitSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecruitSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recruit scheduler started. Active window: 08:00~18:59 (KST), hourly on the hour.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = GetKstNow();
                var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
                var delay = next - now;
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.FromSeconds(1);
                }

                _logger.LogInformation("Next scheduler tick at {NextTickKst:yyyy-MM-dd HH:mm:ss} KST", next);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var tickNow = GetKstNow();
                if (tickNow.Hour < 8 || tickNow.Hour > 18)
                {
                    _logger.LogInformation("Skipped tick at {TickTimeKst:yyyy-MM-dd HH:mm:ss} KST (outside window).", tickNow);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<RecruitSyncService>();
                    await syncService.RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in RecruitSyncBackgroundService tick.");
                }
            }

            _logger.LogInformation("Recruit scheduler stopped.");
        }

        private static DateTime GetKstNow()
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, KoreaTimeZone).DateTime;
        }

        private static TimeZoneInfo ResolveKoreaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
                }
                catch
                {
                    return TimeZoneInfo.Local;
                }
            }
        }
    }
}

