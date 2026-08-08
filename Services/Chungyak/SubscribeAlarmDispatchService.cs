using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Internal;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// SubscribeAlarmDispatchService 관련 기능을 제공합니다.
    /// </summary>
    public class SubscribeAlarmDispatchService
    {
        private static readonly SemaphoreSlim RunLock = new(1, 1);
        private const int DefaultBatchSize = 100;
        private const int MaxRetryCount = 3;
        private const string AlarmSource = "CHUNGYAK_SUBSCRIBE";

        private readonly DBHelper _dbHelper;
        private readonly ISlackNotifier _slackNotifier;
        private readonly ILogger<SubscribeAlarmDispatchService> _logger;

        public SubscribeAlarmDispatchService(
            DBHelper dbHelper,
            ISlackNotifier slackNotifier,
            ILogger<SubscribeAlarmDispatchService> logger)
        {
            _dbHelper = dbHelper;
            _slackNotifier = slackNotifier;
            _logger = logger;
        }

        /// <summary>
        /// RunOnceAsync 작업을 수행합니다.
        /// </summary>
        public async Task<SubscribeAlarmDispatchResponseDto> RunOnceAsync(CancellationToken cancellationToken)
        {
            const string actionName = "DispatchSubscribeAlarm";

            if (!await RunLock.WaitAsync(0, cancellationToken))
            {
                return new SubscribeAlarmDispatchResponseDto
                {
                    Success = false,
                    Skipped = true,
                    Message = "Subscribe alarm dispatch is already running."
                };
            }

            try
            {
                var targets = _dbHelper.ClaimDueSubscribeAlarms(DefaultBatchSize, MaxRetryCount);
                if (targets.Count == 0)
                {
                    return new SubscribeAlarmDispatchResponseDto
                    {
                        Success = true,
                        Message = "No due subscribe alarms.",
                        ClaimedCount = 0
                    };
                }

                var successCount = 0;
                var failCount = 0;
                var skippedCount = 0;

                foreach (var target in targets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var message = BuildMessage(target);
                    var sendResult = await _slackNotifier.SendAsync(message, cancellationToken);

                    try
                    {
                        if (string.Equals(sendResult.SendStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                        {
                            _dbHelper.MarkSubscribeAlarmSuccess(target.Idx);
                            successCount++;
                        }
                        else
                        {
                            var isSkipped = string.Equals(sendResult.SendStatus, "SKIPPED", StringComparison.OrdinalIgnoreCase);
                            _dbHelper.MarkSubscribeAlarmResult(
                                target.Idx,
                                isSkipped ? "SKIPPED" : "FAIL",
                                sendResult.ErrorMessage,
                                increaseRetryCount: !isSkipped);

                            if (isSkipped)
                            {
                                skippedCount++;
                            }
                            else
                            {
                                failCount++;
                            }
                        }
                    }
                    catch (Exception statusEx)
                    {
                        failCount++;
                        _logger.LogWarning(statusEx, "Failed to update subscribe alarm status. idx={Idx}", target.Idx);
                    }

                    try
                    {
                        _dbHelper.SaveAlarmLog(
                            alarmSource: AlarmSource,
                            subscribeIdx: target.SubscribeIdx,
                            pblancId: target.PblancId,
                            alarmType: target.AlarmType,
                            targetDate: target.TargetDate == DateTime.MinValue ? null : target.TargetDate.Date,
                            sendStatus: sendResult.SendStatus,
                            alarmTitle: target.AlarmTitle,
                            alarmMessage: message,
                            errorMessage: sendResult.ErrorMessage);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "Failed to write TB_ALARM_LOG for subscribe alarm. idx={Idx}", target.Idx);
                    }
                }

                var summary = $"claimed:{targets.Count}, success:{successCount}, fail:{failCount}, skipped:{skippedCount}";
                _dbHelper.SaveAccLog(actionName, "10", summary);

                return new SubscribeAlarmDispatchResponseDto
                {
                    Success = true,
                    Message = "Subscribe alarm dispatch completed.",
                    ClaimedCount = targets.Count,
                    SuccessCount = successCount,
                    FailCount = failCount,
                    SkippedCount = skippedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscribe alarm dispatch failed.");
                TrySaveFailLog(actionName, ex.Message);
                return new SubscribeAlarmDispatchResponseDto
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
                _logger.LogWarning(ex, "Failed to write dispatch fail log.");
            }
        }

        private static string BuildMessage(SubscribeAlarmDispatchItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.AlarmMessage))
            {
                return item.AlarmMessage!;
            }

            var targetDateText = item.TargetDate == DateTime.MinValue ? "-" : item.TargetDate.ToString("yyyy-MM-dd");
            return
                $"{item.AlarmTitle ?? "[청약 알림]"}\n" +
                $"- 공고 ID: {item.PblancId}\n" +
                $"- 알림 유형: {item.AlarmType}\n" +
                $"- 기준일: {targetDateText}";
        }
    }
}

