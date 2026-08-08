using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Data.Chungyak
{
    public partial class DBHelper
    {
        public List<AlarmLogResponseDto> GetAlarmLogs(
            DateTime? sendFrom,
            DateTime? sendTo,
            string? sendStatus,
            string? alarmType,
            string? alarmSource,
            string? pblancId)
        {
            var result = new List<AlarmLogResponseDto>();
            var sql = new StringBuilder(@"
                SELECT
                    IDX,
                    ALARM_SOURCE,
                    SUBSCRIBE_IDX,
                    PBLANC_ID,
                    ALARM_TYPE,
                    TARGET_DATE,
                    SEND_STATUS,
                    SEND_TIME,
                    ALARM_TITLE,
                    ALARM_MESSAGE,
                    ERROR_MESSAGE,
                    CREATE_TIME
                FROM dbo.TB_ALARM_LOG
                WHERE 1 = 1
            ");

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            if (sendFrom.HasValue)
            {
                sql.AppendLine("AND CAST(SEND_TIME AS date) >= @SEND_FROM");
                cmd.Parameters.Add("@SEND_FROM", SqlDbType.Date).Value = sendFrom.Value.Date;
            }

            if (sendTo.HasValue)
            {
                sql.AppendLine("AND CAST(SEND_TIME AS date) <= @SEND_TO");
                cmd.Parameters.Add("@SEND_TO", SqlDbType.Date).Value = sendTo.Value.Date;
            }

            if (!string.IsNullOrWhiteSpace(sendStatus))
            {
                sql.AppendLine("AND SEND_STATUS = @SEND_STATUS");
                cmd.Parameters.Add("@SEND_STATUS", SqlDbType.NVarChar, 20).Value = sendStatus;
            }

            if (!string.IsNullOrWhiteSpace(alarmType))
            {
                sql.AppendLine("AND ALARM_TYPE = @ALARM_TYPE");
                cmd.Parameters.Add("@ALARM_TYPE", SqlDbType.NVarChar, 30).Value = alarmType;
            }

            if (!string.IsNullOrWhiteSpace(alarmSource))
            {
                sql.AppendLine("AND ALARM_SOURCE = @ALARM_SOURCE");
                cmd.Parameters.Add("@ALARM_SOURCE", SqlDbType.NVarChar, 30).Value = alarmSource;
            }

            if (!string.IsNullOrWhiteSpace(pblancId))
            {
                sql.AppendLine("AND PBLANC_ID = @PBLANC_ID");
                cmd.Parameters.Add("@PBLANC_ID", SqlDbType.NVarChar, 20).Value = pblancId;
            }

            sql.AppendLine("ORDER BY IDX DESC;");
            cmd.CommandText = sql.ToString();

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new AlarmLogResponseDto
                {
                    Idx = (long)reader["IDX"],
                    AlarmSource = reader["ALARM_SOURCE"]?.ToString() ?? string.Empty,
                    SubscribeIdx = reader["SUBSCRIBE_IDX"] == DBNull.Value ? null : (int?)reader["SUBSCRIBE_IDX"],
                    PblancId = reader["PBLANC_ID"]?.ToString() ?? string.Empty,
                    AlarmType = reader["ALARM_TYPE"]?.ToString() ?? string.Empty,
                    TargetDate = reader["TARGET_DATE"] == DBNull.Value ? null : (DateTime?)reader["TARGET_DATE"],
                    SendStatus = reader["SEND_STATUS"]?.ToString() ?? string.Empty,
                    SendTime = (DateTime)reader["SEND_TIME"],
                    AlarmTitle = reader["ALARM_TITLE"] == DBNull.Value ? null : reader["ALARM_TITLE"]?.ToString(),
                    AlarmMessage = reader["ALARM_MESSAGE"] == DBNull.Value ? null : reader["ALARM_MESSAGE"]?.ToString(),
                    ErrorMessage = reader["ERROR_MESSAGE"] == DBNull.Value ? null : reader["ERROR_MESSAGE"]?.ToString(),
                    CreateTime = (DateTime)reader["CREATE_TIME"]
                });
            }

            return result;
        }
    }
}
