using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Services.Schedules
{
    /// <summary>
    /// RecruitSyncBackgroundService 관련 기능을 제공합니다.
    /// </summary>
    public class RecruitSyncBackgroundService : BackgroundService
    {
        private static readonly TimeZoneInfo KoreaTimeZone = ResolveKoreaTimeZone();
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);
        private const int MaxRetryCount = 3;

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
            _logger.LogInformation("Recruit scheduler started. Active window: 08:00~20:59 (KST), hourly on the hour.");

            try
            {
                _logger.LogInformation("Running initial recruit sync on scheduler startup.");
                await ExecuteSyncWithRetriesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Recruit scheduler stopped during initial sync.");
                return;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Initial recruit sync was cancelled without host shutdown. Scheduler will continue.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in initial recruit sync. Scheduler will continue.");
            }

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
                if (tickNow.Hour < 8 || tickNow.Hour > 20)
                {
                    _logger.LogInformation("Skipped tick at {TickTimeKst:yyyy-MM-dd HH:mm:ss} KST (outside window).", tickNow);
                    continue;
                }

                try
                {
                    await ExecuteSyncWithRetriesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Recruit sync tick was cancelled without host shutdown. Scheduler will continue.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in RecruitSyncBackgroundService tick.");
                }
            }

            _logger.LogInformation("Recruit scheduler stopped.");
        }

        private async Task ExecuteSyncWithRetriesAsync(CancellationToken stoppingToken)
        {
            for (var attempt = 0; attempt <= MaxRetryCount; attempt++)
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<RecruitSyncService>();
                var result = await syncService.RunOnceAsync(stoppingToken);

                _logger.LogInformation(
                    "Recruit sync tick attempt {Attempt}/{MaxAttempt} completed. Success={Success}, Skipped={Skipped}, Total={TotalCount}, Insert={InsertCount}, Update={UpdateCount}, None={NoneCount}, Error={ErrorCount}, Message={Message}",
                    attempt + 1,
                    MaxRetryCount + 1,
                    result.Success,
                    result.Skipped,
                    result.TotalCount,
                    result.InsertCount,
                    result.UpdateCount,
                    result.NoneCount,
                    result.ErrorCount,
                    result.Message);

                if (result.Success || result.Skipped)
                {
                    return;
                }

                if (attempt == MaxRetryCount)
                {
                    _logger.LogWarning(
                        "Recruit sync failed after {AttemptCount} attempts. Scheduler will retry at the next hourly tick.",
                        MaxRetryCount + 1);
                    return;
                }

                _logger.LogWarning(
                    "Recruit sync attempt {Attempt}/{MaxAttempt} failed. Retrying in {RetryDelaySeconds} seconds.",
                    attempt + 1,
                    MaxRetryCount + 1,
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, stoppingToken);
            }
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

