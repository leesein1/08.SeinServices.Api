using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Data.Chungyak
{
    public partial class DBHelper
    {
        public List<ScheduleLogResponseDto> GetScheduleLogs(
            DateTime? startedFrom,
            DateTime? startedTo,
            string? status,
            byte? jobCode)
        {
            var result = new List<ScheduleLogResponseDto>();

            var sql = new StringBuilder(@"
                SELECT
                    IDX,
                    JOB_CODE,
                    JOB_DESC,
                    STATUS,
                    STARTED_AT,
                    ENDED_AT,
                    SCHEDULE_NOTE
                FROM dbo.TB_SCH_LOG
                WHERE 1 = 1
            ");

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            if (startedFrom.HasValue)
            {
                sql.AppendLine("AND CAST(STARTED_AT AS date) >= @STARTED_FROM");
                cmd.Parameters.Add("@STARTED_FROM", SqlDbType.Date).Value = startedFrom.Value.Date;
            }

            if (startedTo.HasValue)
            {
                sql.AppendLine("AND CAST(STARTED_AT AS date) <= @STARTED_TO");
                cmd.Parameters.Add("@STARTED_TO", SqlDbType.Date).Value = startedTo.Value.Date;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                sql.AppendLine("AND STATUS = @STATUS");
                cmd.Parameters.AddWithValue("@STATUS", status);
            }

            if (jobCode.HasValue)
            {
                sql.AppendLine("AND JOB_CODE = @JOB_CODE");
                cmd.Parameters.AddWithValue("@JOB_CODE", jobCode.Value);
            }

            sql.AppendLine("ORDER BY IDX DESC;");
            cmd.CommandText = sql.ToString();

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dbJobCode = (byte)reader["JOB_CODE"];

                result.Add(new ScheduleLogResponseDto
                {
                    Idx = (long)reader["IDX"],
                    JobCode = dbJobCode,
                    JobCodeName = dbJobCode == 1 ? "SYNC" : "CLOSE",
                    JobDesc = reader["JOB_DESC"]?.ToString() ?? string.Empty,
                    Status = reader["STATUS"]?.ToString() ?? string.Empty,
                    StartedAt = (DateTime)reader["STARTED_AT"],
                    EndedAt = reader["ENDED_AT"] == DBNull.Value ? null : (DateTime?)reader["ENDED_AT"],
                    ScheduleNote = reader["SCHEDULE_NOTE"] == DBNull.Value
                        ? null
                        : reader["SCHEDULE_NOTE"]?.ToString()
                });
            }

            return result;
        }
    }
}
