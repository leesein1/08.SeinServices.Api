using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Enums;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        public (string BrtcNm, string SignguNm)? GetFavoriteRegion(string pblancId)
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT TOP 1
                    ISNULL(BRTC_NM, '') AS BRTC_NM,
                    ISNULL(SIGNGU_NM, '') AS SIGNGU_NM
                FROM dbo.TB_RCVHOME
                WHERE PBLANC_ID = @pblancId;";
            cmd.Parameters.AddWithValue("@pblancId", pblancId);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return (
                reader["BRTC_NM"]?.ToString() ?? string.Empty,
                reader["SIGNGU_NM"]?.ToString() ?? string.Empty);
        }

        /// <summary>
        /// SetFavorite 작업을 수행합니다.
        /// </summary>
        public FavoriteResult SetFavorite(string pblancId, bool isFavorite, string brtcNm, string signguNm)
        {
            using var conn = CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            if (isFavorite)
            {
                const string existsSql = @"SELECT COUNT(1) FROM dbo.TB_SUBSCRIBE WHERE PBLANC_ID = @PBLANC_ID;";
                using (var existsCmd = new SqlCommand(existsSql, conn, tx))
                {
                    existsCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);
                    var exists = Convert.ToInt32(existsCmd.ExecuteScalar());
                    if (exists > 0)
                    {
                        tx.Commit();
                        return FavoriteResult.AlreadyAdded;
                    }
                }

                var insertSql = $@"
                    INSERT INTO dbo.TB_SUBSCRIBE (PBLANC_ID, ALERT_DATE, CREATE_TIME, BRTC_NM, SIGNGU_NM)
                    VALUES (@PBLANC_ID, {KstNowSql}, {KstNowSql}, @BRTC_NM, @SIGNGU_NM);
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                int subscribeIdx;
                using (var insertCmd = new SqlCommand(insertSql, conn, tx))
                {
                    insertCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);
                    insertCmd.Parameters.AddWithValue("@BRTC_NM", brtcNm);
                    insertCmd.Parameters.AddWithValue("@SIGNGU_NM", signguNm);

                    var insertedId = insertCmd.ExecuteScalar();
                    subscribeIdx = insertedId is int id ? id : Convert.ToInt32(insertedId);
                }

                var insertAlarmSql = $@"
                    ;WITH Base AS (
                        SELECT
                            COALESCE(
                                TRY_CONVERT(date, r.BEGIN_DE),
                                TRY_CONVERT(date, NULLIF(CONVERT(nvarchar(20), r.BEGIN_DE), ''), 112)
                            ) AS BEGIN_DATE,
                            COALESCE(
                                TRY_CONVERT(date, r.END_DE),
                                TRY_CONVERT(date, NULLIF(CONVERT(nvarchar(20), r.END_DE), ''), 112)
                            ) AS END_DATE,
                            COALESCE(
                                TRY_CONVERT(date, r.PRZWNER_PRESNATN_DE),
                                TRY_CONVERT(date, NULLIF(CONVERT(nvarchar(20), r.PRZWNER_PRESNATN_DE), ''), 112)
                            ) AS WINNER_DATE
                        FROM dbo.TB_RCVHOME r
                        WHERE r.PBLANC_ID = @PBLANC_ID
                    ),
                    Candidate AS (
                        SELECT
                            v.ALARM_TYPE,
                            CAST(v.TARGET_DATE AS date) AS TARGET_DATE,
                            v.SORT_ORDER
                        FROM Base b
                        CROSS APPLY (VALUES
                            (N'RECRUIT_BEGIN_D1', DATEADD(day, -1, b.BEGIN_DATE), 1),
                            (N'RECRUIT_BEGIN_DDAY', b.BEGIN_DATE, 2),
                            (N'RECRUIT_END_D1', DATEADD(day, -1, b.END_DATE), 3),
                            (N'RECRUIT_END_DDAY', b.END_DATE, 4),
                            (N'WINNER_ANNOUNCE', b.WINNER_DATE, 5)
                        ) v(ALARM_TYPE, TARGET_DATE, SORT_ORDER)
                        WHERE v.TARGET_DATE IS NOT NULL
                          AND CAST(v.TARGET_DATE AS date) >= {KstTodaySql}
                    ),
                    Dedup AS (
                        SELECT
                            ALARM_TYPE,
                            TARGET_DATE,
                            ROW_NUMBER() OVER (PARTITION BY TARGET_DATE ORDER BY SORT_ORDER) AS RN
                        FROM Candidate
                    )
                    INSERT INTO dbo.TB_SUBSCRIBE_ALARM
                    (
                        SUBSCRIBE_IDX,
                        PBLANC_ID,
                        ALARM_TYPE,
                        TARGET_DATE,
                        ALARM_TITLE,
                        ALARM_MESSAGE,
                        CREATE_TIME,
                        SEND_AT,
                        STATUS,
                        RETRY_COUNT,
                        LAST_ERROR
                    )
                    SELECT
                        @SUBSCRIBE_IDX,
                        @PBLANC_ID,
                        d.ALARM_TYPE,
                        d.TARGET_DATE,
                        CASE d.ALARM_TYPE
                            WHEN N'RECRUIT_BEGIN_D1' THEN N'[청약 접수 하루 전 알림]'
                            WHEN N'RECRUIT_BEGIN_DDAY' THEN N'[청약 접수 시작 알림]'
                            WHEN N'RECRUIT_END_D1' THEN N'[청약 접수 마감 하루 전 알림]'
                            WHEN N'RECRUIT_END_DDAY' THEN N'[청약 접수 마감일 알림]'
                            WHEN N'WINNER_ANNOUNCE' THEN N'[청약 당첨자 발표 알림]'
                            ELSE N'[청약 알림]'
                        END,
                        CONCAT(N'PBLANC_ID: ', @PBLANC_ID, N', 알림일: ', CONVERT(nvarchar(10), d.TARGET_DATE, 23)),
                        {KstNowSql},
                        DATEADD(hour, 9, CAST(d.TARGET_DATE AS datetime)),
                        N'READY',
                        0,
                        NULL
                    FROM Dedup d
                    WHERE d.RN = 1;";

                using (var alarmCmd = new SqlCommand(insertAlarmSql, conn, tx))
                {
                    alarmCmd.Parameters.AddWithValue("@SUBSCRIBE_IDX", subscribeIdx);
                    alarmCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);
                    alarmCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return FavoriteResult.Added;
            }

            const string deleteAlarmSql = @"DELETE FROM dbo.TB_SUBSCRIBE_ALARM WHERE PBLANC_ID = @PBLANC_ID;";
            using (var deleteAlarmCmd = new SqlCommand(deleteAlarmSql, conn, tx))
            {
                deleteAlarmCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);
                deleteAlarmCmd.ExecuteNonQuery();
            }

            const string deleteSubscribeSql = @"DELETE FROM dbo.TB_SUBSCRIBE WHERE PBLANC_ID = @PBLANC_ID;";
            using var deleteCmd = new SqlCommand(deleteSubscribeSql, conn, tx);
            deleteCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);

            var deleted = deleteCmd.ExecuteNonQuery();
            tx.Commit();
            return deleted > 0 ? FavoriteResult.Removed : FavoriteResult.AlreadyRemoved;
        }
    }
}
