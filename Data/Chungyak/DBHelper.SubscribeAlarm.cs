using System.Data;
using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Internal;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        /// <summary>
        /// ClaimDueSubscribeAlarms 작업을 수행합니다.
        /// </summary>
        public List<SubscribeAlarmDispatchItem> ClaimDueSubscribeAlarms(int batchSize, int maxRetryCount)
        {
            var result = new List<SubscribeAlarmDispatchItem>();

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = $@"
                ;WITH due AS
                (
                    SELECT TOP (@BATCH_SIZE) *
                    FROM dbo.TB_SUBSCRIBE_ALARM WITH (READPAST, UPDLOCK, ROWLOCK)
                    WHERE SEND_AT <= {KstNowSql}
                      AND STATUS IN (N'READY', N'FAIL')
                      AND RETRY_COUNT < @MAX_RETRY_COUNT
                    ORDER BY SEND_AT ASC, IDX ASC
                )
                UPDATE due
                    SET STATUS = N'PROCESSING'
                OUTPUT
                    INSERTED.IDX,
                    INSERTED.SUBSCRIBE_IDX,
                    INSERTED.PBLANC_ID,
                    INSERTED.ALARM_TYPE,
                    INSERTED.TARGET_DATE,
                    INSERTED.ALARM_TITLE,
                    INSERTED.ALARM_MESSAGE,
                    INSERTED.SEND_AT,
                    INSERTED.RETRY_COUNT,
                    INSERTED.STATUS;";

            cmd.Parameters.Add("@BATCH_SIZE", SqlDbType.Int).Value = batchSize;
            cmd.Parameters.Add("@MAX_RETRY_COUNT", SqlDbType.Int).Value = maxRetryCount;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SubscribeAlarmDispatchItem
                {
                    Idx = (long)reader["IDX"],
                    SubscribeIdx = reader["SUBSCRIBE_IDX"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SUBSCRIBE_IDX"]),
                    PblancId = reader["PBLANC_ID"]?.ToString() ?? string.Empty,
                    AlarmType = reader["ALARM_TYPE"]?.ToString() ?? string.Empty,
                    TargetDate = reader["TARGET_DATE"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["TARGET_DATE"]),
                    AlarmTitle = reader["ALARM_TITLE"] == DBNull.Value ? null : reader["ALARM_TITLE"]?.ToString(),
                    AlarmMessage = reader["ALARM_MESSAGE"] == DBNull.Value ? null : reader["ALARM_MESSAGE"]?.ToString(),
                    SendAt = reader["SEND_AT"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["SEND_AT"]),
                    RetryCount = reader["RETRY_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RETRY_COUNT"]),
                    Status = reader["STATUS"] == DBNull.Value ? null : reader["STATUS"]?.ToString()
                });
            }

            return result;
        }

        /// <summary>
        /// MarkSubscribeAlarmSuccess 작업을 수행합니다.
        /// </summary>
        public void MarkSubscribeAlarmSuccess(long idx)
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE dbo.TB_SUBSCRIBE_ALARM
                SET STATUS = N'SUCCESS',
                    LAST_ERROR = NULL
                WHERE IDX = @IDX;";

            cmd.Parameters.Add("@IDX", SqlDbType.BigInt).Value = idx;
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// MarkSubscribeAlarmResult 작업을 수행합니다.
        /// </summary>
        public void MarkSubscribeAlarmResult(long idx, string status, string? errorMessage, bool increaseRetryCount)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentNullException(nameof(status));
            }

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                UPDATE dbo.TB_SUBSCRIBE_ALARM
                SET STATUS = @STATUS,
                    RETRY_COUNT = RETRY_COUNT + CASE WHEN @INCREASE_RETRY = 1 THEN 1 ELSE 0 END,
                    LAST_ERROR = @LAST_ERROR
                WHERE IDX = @IDX;";

            cmd.Parameters.Add("@IDX", SqlDbType.BigInt).Value = idx;
            cmd.Parameters.Add("@STATUS", SqlDbType.NVarChar, 20).Value = status.Trim().ToUpperInvariant();
            cmd.Parameters.Add("@INCREASE_RETRY", SqlDbType.Bit).Value = increaseRetryCount;
            cmd.Parameters.Add("@LAST_ERROR", SqlDbType.NVarChar, 2000).Value = (object?)TrimToMax(errorMessage, 2000) ?? DBNull.Value;

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}

