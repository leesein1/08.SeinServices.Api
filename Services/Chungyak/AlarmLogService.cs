using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// AlarmLogService 관련 기능을 제공합니다.
    /// </summary>
    public class AlarmLogService
    {
        private static readonly HashSet<string> AllowedSendStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "SUCCESS",
            "FAIL",
            "SKIPPED"
        };

        private readonly DBHelper _dbHelper;

        public AlarmLogService(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public bool IsValidDateRange(DateTime? sendFrom, DateTime? sendTo)
        {
            if (!sendFrom.HasValue || !sendTo.HasValue)
            {
                return true;
            }

            return sendFrom.Value.Date <= sendTo.Value.Date;
        }

        public bool IsValidSendStatus(string? sendStatus)
        {
            if (string.IsNullOrWhiteSpace(sendStatus))
            {
                return true;
            }

            return AllowedSendStatuses.Contains(sendStatus.Trim());
        }

        public List<AlarmLogResponseDto> GetAlarmLogs(AlarmLogSearchRequestDto request)
        {
            return _dbHelper.GetAlarmLogs(
                request.SendFrom,
                request.SendTo,
                Normalize(request.SendStatus),
                Normalize(request.AlarmType),
                Normalize(request.AlarmSource),
                Normalize(request.PblancId));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
