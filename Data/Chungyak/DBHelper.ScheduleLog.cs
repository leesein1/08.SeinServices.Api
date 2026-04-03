using Microsoft.Data.SqlClient;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        /// <summary>
        /// SaveScheduleLog 작업을 수행합니다.
        /// </summary>
        public void SaveScheduleLog(
            byte jobCode,
            string status,
            DateTime startedAtUtc,
            DateTime? endedAtUtc,
            string? scheduleNote = null)
        {
            if (jobCode is not 1 and not 2)
            {
                throw new ArgumentOutOfRangeException(nameof(jobCode), "jobCode must be 1(sync) or 2(close).");
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentNullException(nameof(status));
            }

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO dbo.TB_SCH_LOG
                (
                    JOB_CODE,
                    STATUS,
                    STARTED_AT,
                    ENDED_AT,
                    SCHEDULE_NOTE
                )
                VALUES
                (
                    @JOB_CODE,
                    @STATUS,
                    @STARTED_AT,
                    @ENDED_AT,
                    @SCHEDULE_NOTE
                );";

            cmd.Parameters.AddWithValue("@JOB_CODE", jobCode);
            cmd.Parameters.AddWithValue("@STATUS", status.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@STARTED_AT", startedAtUtc);
            cmd.Parameters.AddWithValue("@ENDED_AT", (object?)endedAtUtc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SCHEDULE_NOTE", (object?)scheduleNote ?? DBNull.Value);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
