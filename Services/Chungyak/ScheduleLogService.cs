using SeinServices.Api.Data.Chungyak;
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
        /// TryMapJobTypeToCode 작업을 수행합니다.
        /// </summary>
        public bool TryMapJobTypeToCode(int jobType, out byte jobCode)
        {
            jobCode = 0;

            if (jobType == 1)
            {
                jobCode = 1; // sync(main)
                return true;
            }

            if (jobType == 0)
            {
                jobCode = 2; // close
                return true;
            }

            return false;
        }

        /// <summary>
        /// GetLastScheduleLog 작업을 수행합니다.
        /// </summary>
        public ScheduleLastResponseDto? GetLastScheduleLog(int jobType)
        {
            if (!TryMapJobTypeToCode(jobType, out var jobCode))
            {
                return null;
            }

            return _dbHelper.GetLastScheduleLog(jobCode, jobType);
        }
    }
}
