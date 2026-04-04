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
    }
}
