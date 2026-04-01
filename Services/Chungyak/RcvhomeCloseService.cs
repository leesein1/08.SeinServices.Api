using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// RcvhomeCloseService 관련 기능을 제공합니다.
    /// </summary>
    public class RcvhomeCloseService
    {
        private static readonly SemaphoreSlim RunLock = new(1, 1);

        private readonly DBHelper _dbHelper;
        private readonly ILogger<RcvhomeCloseService> _logger;

        public RcvhomeCloseService(
            DBHelper dbHelper,
            ILogger<RcvhomeCloseService> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// RunOnceAsync 작업을 수행합니다.
        /// </summary>
        public async Task<CloseRunResponseDto> RunOnceAsync(CancellationToken cancellationToken)
        {
            const string actionName = "CloseRcvhome";

            if (!await RunLock.WaitAsync(0, cancellationToken))
            {
                return new CloseRunResponseDto
                {
                    Success = false,
                    Skipped = true,
                    Message = "Close job is already running."
                };
            }

            try
            {
                var closedCount = _dbHelper.CloseRcvhome();
                _dbHelper.SaveAccLog(actionName, "10", $"Closed:{closedCount}");

                _logger.LogInformation("Rcvhome close completed. closed={ClosedCount}", closedCount);
                return new CloseRunResponseDto
                {
                    Success = true,
                    Message = "Close completed.",
                    ClosedCount = closedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rcvhome close failed.");
                TrySaveFailLog(actionName, ex.Message);
                return new CloseRunResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            finally
            {
                RunLock.Release();
            }
        }

        private void TrySaveFailLog(string actionName, string message)
        {
            try
            {
                _dbHelper.SaveAccLog(actionName, "00", message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write close fail log.");
            }
        }
    }
}

