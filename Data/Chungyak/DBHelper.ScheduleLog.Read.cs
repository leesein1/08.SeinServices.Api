using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        /// <summary>
        /// GetLastScheduleLog 작업을 수행합니다.
        /// </summary>
        public ScheduleLastResponseDto? GetLastScheduleLog(byte jobCode, int jobType)
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT TOP 1
                    JOB_CODE,
                    STATUS,
                    STARTED_AT,
                    ENDED_AT,
                    SCHEDULE_NOTE
                FROM dbo.TB_SCH_LOG
                WHERE JOB_CODE = @JOB_CODE
                ORDER BY IDX DESC;";

            cmd.Parameters.AddWithValue("@JOB_CODE", jobCode);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            var startedAt = (DateTime)reader["STARTED_AT"];
            var endedAt = reader["ENDED_AT"] == DBNull.Value ? null : (DateTime?)reader["ENDED_AT"];

            return new ScheduleLastResponseDto
            {
                JobType = jobType,
                JobCode = jobCode,
                Status = reader["STATUS"]?.ToString() ?? string.Empty,
                StartedAt = startedAt,
                EndedAt = endedAt,
                LastCommunicatedAt = endedAt ?? startedAt,
                ScheduleNote = reader["SCHEDULE_NOTE"] == DBNull.Value
                    ? null
                    : reader["SCHEDULE_NOTE"]?.ToString()
            };
        }
    }
}
