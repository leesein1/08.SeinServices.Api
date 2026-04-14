using SeinServices.Api.Data.Chungyak;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// ScheduleLogService 관련 기능을 제공합니다.
    /// </summary>
    public class ScheduleLogService
    {
        private readonly DBHelper _dbHelper;

        public ScheduleLogService(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// TryMapJobCodeToType 작업을 수행합니다.
        /// </summary>
        public bool TryMapJobCodeToType(byte jobCode, out int jobType)
        {
            jobType = -1;

            if (jobCode == 1)
            {
                jobType = 1; // sync(main)
                return true;
            }

            if (jobCode == 2)
            {
                jobType = 0; // close
                return true;
            }

            return false;
        }

        /// <summary>
        /// GetLastScheduleLogByJobCode 작업을 수행합니다.
        /// </summary>
        public ScheduleLastResponseDto? GetLastScheduleLogByJobCode(byte jobCode)
        {
            if (!TryMapJobCodeToType(jobCode, out var jobType))
            {
                return null;
            }

            return _dbHelper.GetLastScheduleLog(jobCode, jobType);
        }

        public bool IsValidStatusFilter(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return true;
            }

            var normalized = status.Trim().ToUpperInvariant();
            return normalized is "SUCCESS" or "FAILED";
        }

        public bool IsValidDateRange(DateTime? startedFrom, DateTime? startedTo)
        {
            if (!startedFrom.HasValue || !startedTo.HasValue)
            {
                return true;
            }

            return startedFrom.Value.Date <= startedTo.Value.Date;
        }

        public bool TryMapJobCode(string? jobCode, out byte? mappedJobCode)
        {
            mappedJobCode = null;

            if (string.IsNullOrWhiteSpace(jobCode))
            {
                return true;
            }

            var normalized = jobCode.Trim().ToUpperInvariant();
            if (normalized == "SYNC")
            {
                mappedJobCode = 1;
                return true;
            }

            if (normalized == "CLOSE")
            {
                mappedJobCode = 2;
                return true;
            }

            return false;
        }

        public List<ScheduleLogResponseDto> GetScheduleLogs(ScheduleLogSearchRequestDto request)
        {
            var normalizedStatus = string.IsNullOrWhiteSpace(request.Status)
                ? null
                : request.Status.Trim().ToUpperInvariant();

            TryMapJobCode(request.JobCode, out var mappedJobCode);

            return _dbHelper.GetScheduleLogs(
                request.StartedFrom,
                request.StartedTo,
                normalizedStatus,
                mappedJobCode);
        }
    }
}
