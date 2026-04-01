using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SeinServices.Api.Data.Chungyak
{
    public partial class DBHelper
    {
        /// <summary>
        /// 즐겨찾기 목록만 조회합니다.
        /// </summary>
        public DataTable GetRcvHomeFav(string keyword, string status, DateTime beginFrom, DateTime beginTo)
        {
            var dt = new DataTable();
            var sql = new StringBuilder();

            sql.AppendLine(@"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY a.BEGIN_DE DESC) AS 순서,
                    a.PBLANC_ID AS 고유번호,
                    a.PBLANC_NM AS 공고명,
                    a.HSMP_NM   AS 단지명,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN ISNULL(a.STTUS_NM, N'')
                        WHEN CAST(GETDATE() AS date) < a.BEGIN_DE THEN N'접수예정'
                        WHEN CAST(GETDATE() AS date) > a.END_DE THEN N'접수마감'
                        ELSE N'접수중'
                    END AS 상태,
                    a.BEGIN_DE  AS 접수시작일,
                    a.END_DE    AS 접수마감일,
                    CONVERT(nvarchar(10), a.BEGIN_DE, 23) + N' ~ ' + CONVERT(nvarchar(10), a.END_DE, 23) AS 접수기간,
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(a.FULL_ADRES)), N'') IS NOT NULL THEN a.FULL_ADRES
                        ELSE CONCAT_WS(N' ', a.BRTC_NM, a.SIGNGU_NM)
                    END AS 주소,
                    a.HOUSE_TY_NM AS 공급유형,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN N''
                        WHEN CAST(GETDATE() AS date) < a.BEGIN_DE
                            THEN N'D-' + CAST(DATEDIFF(day, CAST(GETDATE() AS date), a.BEGIN_DE) AS nvarchar(10))
                        WHEN CAST(GETDATE() AS date) > a.END_DE
                            THEN N'마감'
                        ELSE N'접수중'
                    END AS 남은일수,
                    a.PC_URL AS URL,
                    CAST(1 AS bit) AS 즐겨찾기
                FROM dbo.TB_SUBSCRIBE b
                LEFT JOIN dbo.TB_RCVHOME a ON b.PBLANC_ID = a.PBLANC_ID
                WHERE 1 = 1
            ");

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql.AppendLine(@"
                    AND (
                        a.PBLANC_NM  LIKE @keyword OR
                        a.HSMP_NM    LIKE @keyword OR
                        a.FULL_ADRES LIKE @keyword OR
                        a.SIGNGU_NM  LIKE @keyword OR
                        a.BRTC_NM    LIKE @keyword
                    )");
                cmd.Parameters.AddWithValue("@keyword", $"%{keyword.Trim()}%");
            }

            var isFilteredStatus = !string.IsNullOrWhiteSpace(status) && status != "전체";
            if (isFilteredStatus)
            {
                if (status == "접수예정")
                {
                    sql.AppendLine("AND CAST(GETDATE() AS date) < a.BEGIN_DE");
                }
                else if (status == "접수중")
                {
                    sql.AppendLine("AND CAST(GETDATE() AS date) BETWEEN a.BEGIN_DE AND a.END_DE");
                }
                else if (status == "접수마감")
                {
                    sql.AppendLine("AND CAST(GETDATE() AS date) > a.END_DE");
                }
            }

            var dateColumn = status == "접수마감" ? "a.END_DE" : "a.BEGIN_DE";
            sql.AppendLine($"AND {dateColumn} >= @beginFrom AND {dateColumn} <= @beginTo");
            cmd.Parameters.Add("@beginFrom", SqlDbType.Date).Value = beginFrom.Date;
            cmd.Parameters.Add("@beginTo", SqlDbType.Date).Value = beginTo.Date;

            sql.AppendLine("ORDER BY a.BEGIN_DE DESC;");

            cmd.CommandText = sql.ToString();
            using var da = new SqlDataAdapter(cmd);
            conn.Open();
            da.Fill(dt);

            return dt;
        }
    }
}

