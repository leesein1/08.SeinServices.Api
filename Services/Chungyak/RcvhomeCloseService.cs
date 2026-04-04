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
        private const byte CloseJobCode = 2;

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
            var startedAt = DateTime.UtcNow;

            if (!await RunLock.WaitAsync(0, cancellationToken))
            {
                var skipMessage = "Close job is already running.";
                try
                {
                    SaveScheduleLog(CloseJobCode, "SKIPPED", startedAt, skipMessage);
                }
                catch (Exception logEx)
                {
                    return new CloseRunResponseDto
                    {
                        Success = false,
                        Message = $"{skipMessage} Schedule log save failed: {logEx.Message}"
                    };
                }

                return new CloseRunResponseDto
                {
                    Success = false,
                    Skipped = true,
                    Message = skipMessage
                };
            }

            try
            {
                var closedCount = _dbHelper.CloseRcvhome();
                _dbHelper.SaveAccLog(actionName, "10", $"Closed:{closedCount}");
                SaveScheduleLog(CloseJobCode, "SUCCESS", startedAt, $"마감 {closedCount}건");

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

                try
                {
                    SaveScheduleLog(CloseJobCode, "FAIL", startedAt, TruncateNote(ex.Message));
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to write TB_SCH_LOG for close fail case.");
                    return new CloseRunResponseDto
                    {
                        Success = false,
                        Message = $"{ex.Message} | Schedule log save failed: {logEx.Message}"
                    };
                }

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

        private void SaveScheduleLog(byte jobCode, string status, DateTime startedAt, string? scheduleNote)
        {
            _dbHelper.SaveScheduleLog(
                jobCode,
                status,
                startedAt,
                DateTime.UtcNow,
                TruncateNote(scheduleNote));
        }

        private static string? TruncateNote(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return text.Length <= 200 ? text : text[..200];
        }
    }
}
