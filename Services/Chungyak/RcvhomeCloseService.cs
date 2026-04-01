using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// 모집공고 마감(CLS_YN 갱신) 배치�?처리?�는 ?�비?�입?�다.
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
        /// 마감 배치�?즉시 1???�행?�니??
        /// </summary>
        /// <param name="cancellationToken">취소 ?�큰</param>
        /// <returns>?�행 결과 ?�약</returns>
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

