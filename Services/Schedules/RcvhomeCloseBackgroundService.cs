using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Services.Schedules
{
    /// <summary>
    /// Îß§Ïùº KST ?àÎ≤Ω 03:00??Î™®ÏßëÍ≥µÍ≥† ÎßàÍ∞ê Î∞∞ÏπòÎ•??§Ìñâ?òÎäî ?§Ï?Ï§ÑÎü¨?ÖÎãà??
    /// </summary>
    public class RcvhomeCloseBackgroundService : BackgroundService
    {
        private static readonly TimeZoneInfo KoreaTimeZone = ResolveKoreaTimeZone();

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RcvhomeCloseBackgroundService> _logger;

        public RcvhomeCloseBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RcvhomeCloseBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// ?àÎ≤Ω ÎßàÍ∞ê ?§Ï?Ï§?Î£®ÌîÑÎ•??§Ìñâ?©Îãà??
        /// </summary>
        /// <param name="stoppingToken">?úÎπÑ??Ï§ëÏ? ?†ÌÅ∞</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Rcvhome close scheduler started. Daily at 03:00 KST.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = GetKstNow();
                var todayRunAt = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0);
                var nextRun = now < todayRunAt ? todayRunAt : todayRunAt.AddDays(1);
                var delay = nextRun - now;
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.FromSeconds(1);
                }

                _logger.LogInformation("Next close tick at {NextTickKst:yyyy-MM-dd HH:mm:ss} KST", nextRun);

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
                    var closeService = scope.ServiceProvider.GetRequiredService<RcvhomeCloseService>();
                    await closeService.RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in RcvhomeCloseBackgroundService tick.");
                }
            }

            _logger.LogInformation("Rcvhome close scheduler stopped.");
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

