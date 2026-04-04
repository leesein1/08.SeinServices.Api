using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.Chungyak.Responses;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        public List<RcvhomeResponseDto> GetRcvHomeD7(
            string? keyword,
            string? status,
            DateTime? beginFrom,
            DateTime? beginTo,
            bool onlyFavorite = false)
        {
            var list = new List<RcvhomeResponseDto>();

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = $@"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY a.BEGIN_DE DESC, a.PBLANC_ID) AS 순서,
                    a.PBLANC_ID AS 고유번호,
                    a.PBLANC_NM AS 공고명,
                    ISNULL(a.HSMP_NM, N'') AS 단지명,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN ISNULL(a.STTUS_NM, N'')
                        WHEN {KstTodaySql} < a.BEGIN_DE THEN N'접수예정'
                        WHEN {KstTodaySql} > a.END_DE THEN N'접수마감'
                        ELSE N'접수중'
                    END AS 상태,
                    a.BEGIN_DE AS 접수시작일,
                    a.END_DE   AS 접수마감일,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN N''
                        ELSE CONVERT(nvarchar(10), a.BEGIN_DE, 23) + N' ~ ' + CONVERT(nvarchar(10), a.END_DE, 23)
                    END AS 접수기간,
                    LTRIM(RTRIM(ISNULL(a.BRTC_NM, N'') + N' ' + ISNULL(a.SIGNGU_NM, N'') + N' ' + ISNULL(a.FULL_ADRES, N''))) AS 주소,
                    ISNULL(a.HOUSE_SN, N'') AS 공급유형,
                    ISNULL(a.URL, N'') AS URL,
                    CASE WHEN s.PBLANC_ID IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS 즐겨찾기,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN N''
                        WHEN {KstTodaySql} < a.BEGIN_DE
                            THEN N'D-' + CAST(DATEDIFF(day, {KstTodaySql}, a.BEGIN_DE) AS nvarchar(10))
                        WHEN {KstTodaySql} > a.END_DE
                            THEN N'마감'
                        ELSE
                            CASE
                                WHEN DATEDIFF(day, {KstTodaySql}, a.END_DE) <= 7
                                    THEN N'D-' + CAST(DATEDIFF(day, {KstTodaySql}, a.END_DE) AS nvarchar(10))
                                ELSE N'접수중'
                            END
                    END AS 남은일수,
                    a.RCRIT_PBLANC_DE AS 공고일
                FROM dbo.TB_RCVHOME a
                LEFT JOIN dbo.TB_SUBSCRIBE s ON s.PBLANC_ID = a.PBLANC_ID
                WHERE 1 = 1
                  AND (
                        (
                            {KstTodaySql} < a.BEGIN_DE
                            AND DATEDIFF(day, {KstTodaySql}, a.BEGIN_DE) <= 7
                        )
                        OR
                        (
                            {KstTodaySql} BETWEEN a.BEGIN_DE AND a.END_DE
                        )
                  )";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                cmd.CommandText += @"
                    AND (
                        a.PBLANC_NM LIKE @keyword OR
                        a.HSMP_NM LIKE @keyword OR
                        a.FULL_ADRES LIKE @keyword OR
                        a.SIGNGU_NM LIKE @keyword OR
                        a.BRTC_NM LIKE @keyword
                    )";
                cmd.Parameters.AddWithValue("@keyword", $"%{keyword.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "전체")
            {
                if (status == "접수예정")
                    cmd.CommandText += $" AND {KstTodaySql} < a.BEGIN_DE";
                else if (status == "접수중")
                    cmd.CommandText += $" AND {KstTodaySql} BETWEEN a.BEGIN_DE AND a.END_DE";
                else if (status == "접수마감")
                    cmd.CommandText += $" AND {KstTodaySql} > a.END_DE";
            }

            if (beginFrom.HasValue && beginTo.HasValue)
            {
                var dateColumn = status == "접수마감" ? "a.END_DE" : "a.BEGIN_DE";
                cmd.CommandText += $" AND {dateColumn} >= @beginFrom AND {dateColumn} <= @beginTo";
                cmd.Parameters.AddWithValue("@beginFrom", beginFrom.Value.Date);
                cmd.Parameters.AddWithValue("@beginTo", beginTo.Value.Date);
            }

            if (onlyFavorite)
            {
                cmd.CommandText += " AND s.PBLANC_ID IS NOT NULL";
            }

            cmd.CommandText += " ORDER BY a.BEGIN_DE DESC, a.PBLANC_ID;";

            conn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new RcvhomeResponseDto
                {
                    순서 = rd.GetInt64(rd.GetOrdinal("순서")),
                    고유번호 = rd["고유번호"]?.ToString() ?? string.Empty,
                    공고명 = rd["공고명"]?.ToString() ?? string.Empty,
                    단지명 = rd["단지명"]?.ToString() ?? string.Empty,
                    상태 = rd["상태"]?.ToString() ?? string.Empty,
                    접수시작일 = rd["접수시작일"] == DBNull.Value ? null : (DateTime?)rd["접수시작일"],
                    접수마감일 = rd["접수마감일"] == DBNull.Value ? null : (DateTime?)rd["접수마감일"],
                    접수기간 = rd["접수기간"]?.ToString() ?? string.Empty,
                    주소 = rd["주소"]?.ToString() ?? string.Empty,
                    공급유형 = rd["공급유형"]?.ToString() ?? string.Empty,
                    남은일수 = rd["남은일수"]?.ToString() ?? string.Empty,
                    URL = rd["URL"]?.ToString() ?? string.Empty,
                    즐겨찾기 = rd["즐겨찾기"] != DBNull.Value && (bool)rd["즐겨찾기"],
                    공고일 = rd["공고일"] == DBNull.Value ? null : (DateTime?)rd["공고일"]
                });
            }

            return list;
        }
    }
}

