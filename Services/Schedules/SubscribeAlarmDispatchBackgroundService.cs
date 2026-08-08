using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Services.Schedules
{
    /// <summary>
    /// Dispatches due subscription alarms every hour on the hour.
    /// </summary>
    public class SubscribeAlarmDispatchBackgroundService : BackgroundService
    {
        private static readonly TimeZoneInfo KoreaTimeZone = ResolveKoreaTimeZone();

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscribeAlarmDispatchBackgroundService> _logger;

        public SubscribeAlarmDispatchBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscribeAlarmDispatchBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscribe alarm dispatch scheduler started. Hourly on the hour (KST).");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = GetKstNow();
                var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
                var delay = next - now;
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.FromSeconds(1);
                }

                _logger.LogInformation("Next subscribe alarm dispatch tick at {NextTickKst:yyyy-MM-dd HH:mm:ss} KST", next);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dispatchService = scope.ServiceProvider.GetRequiredService<SubscribeAlarmDispatchService>();
                    var result = await dispatchService.RunOnceAsync(stoppingToken);

                    _logger.LogInformation(
                        "Subscribe alarm dispatch tick completed. Success={Success}, Skipped={Skipped}, Claimed={ClaimedCount}, Sent={SuccessCount}, Failed={FailCount}",
                        result.Success,
                        result.Skipped,
                        result.ClaimedCount,
                        result.SuccessCount,
                        result.FailCount);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in SubscribeAlarmDispatchBackgroundService tick.");
                }
            }

            _logger.LogInformation("Subscribe alarm dispatch scheduler stopped.");
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
