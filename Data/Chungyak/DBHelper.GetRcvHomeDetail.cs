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
        /// GetRcvHomeDetail 작업을 수행합니다.
        /// </summary>
        public RcvhomeDetailResponseDto? GetRcvHomeDetail(string pblancId)
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = $@"
                SELECT TOP 1
                    a.PBLANC_ID AS PblancId,
                    a.PBLANC_NM AS NoticeName,
                    ISNULL(a.HSMP_NM, N'') AS ComplexName,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN ISNULL(a.STTUS_NM, N'')
                        WHEN {KstTodaySql} < a.BEGIN_DE THEN N'접수예정'
                        WHEN {KstTodaySql} > a.END_DE THEN N'접수마감'
                        ELSE N'접수중'
                    END AS Status,
                    ISNULL(a.STTUS_NM, N'') AS RawStatus,
                    a.BEGIN_DE AS BeginDate,
                    a.END_DE   AS EndDate,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN N''
                        ELSE CONVERT(nvarchar(10), a.BEGIN_DE, 23) + N' ~ ' + CONVERT(nvarchar(10), a.END_DE, 23)
                    END AS DateRangeText,
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(a.FULL_ADRES)), N'') IS NOT NULL THEN a.FULL_ADRES
                        ELSE CONCAT_WS(N' ', a.BRTC_NM, a.SIGNGU_NM)
                    END AS Address,
                    ISNULL(a.BRTC_NM, N'') AS BrtcName,
                    ISNULL(a.SIGNGU_NM, N'') AS SignguName,
                    ISNULL(a.HOUSE_TY_NM, N'') AS HouseTypeName,
                    CASE
                        WHEN a.BEGIN_DE IS NULL OR a.END_DE IS NULL THEN N''
                        WHEN {KstTodaySql} < a.BEGIN_DE
                            THEN N'D-' + CAST(DATEDIFF(day, {KstTodaySql}, a.BEGIN_DE) AS nvarchar(10))
                        WHEN {KstTodaySql} > a.END_DE
                            THEN N'마감'
                        ELSE N'접수중'
                    END AS DdayText,
                    ISNULL(a.PC_URL, N'') AS Url,
                    CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM dbo.TB_SUBSCRIBE AS b
                            WHERE b.PBLANC_ID = a.PBLANC_ID
                        )
                        THEN CAST(1 AS bit)
                        ELSE CAST(0 AS bit)
                    END AS IsFavorite,
                    a.RCRIT_PBLANC_DE AS AnnouncementDate,
                    a.PRZWNER_PRESNATN_DE AS PRZWNER_PRESNATN_DE
                FROM dbo.TB_RCVHOME AS a
                WHERE a.PBLANC_ID = @pblancId;";

            cmd.Parameters.AddWithValue("@pblancId", pblancId);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new RcvhomeDetailResponseDto
            {
                PblancId = reader["PblancId"]?.ToString() ?? string.Empty,
                NoticeName = reader["NoticeName"]?.ToString() ?? string.Empty,
                ComplexName = reader["ComplexName"]?.ToString() ?? string.Empty,
                Status = reader["Status"]?.ToString() ?? string.Empty,
                RawStatus = reader["RawStatus"]?.ToString() ?? string.Empty,
                BeginDate = reader["BeginDate"] == DBNull.Value ? null : (DateTime?)reader["BeginDate"],
                EndDate = reader["EndDate"] == DBNull.Value ? null : (DateTime?)reader["EndDate"],
                DateRangeText = reader["DateRangeText"]?.ToString() ?? string.Empty,
                Address = reader["Address"]?.ToString() ?? string.Empty,
                BrtcName = reader["BrtcName"]?.ToString() ?? string.Empty,
                SignguName = reader["SignguName"]?.ToString() ?? string.Empty,
                HouseTypeName = reader["HouseTypeName"]?.ToString() ?? string.Empty,
                DdayText = reader["DdayText"]?.ToString() ?? string.Empty,
                Url = reader["Url"]?.ToString() ?? string.Empty,
                IsFavorite = reader["IsFavorite"] != DBNull.Value && (bool)reader["IsFavorite"],
                AnnouncementDate = reader["AnnouncementDate"] == DBNull.Value ? null : (DateTime?)reader["AnnouncementDate"],
                PRZWNER_PRESNATN_DE = reader["PRZWNER_PRESNATN_DE"] == DBNull.Value ? null : (DateTime?)reader["PRZWNER_PRESNATN_DE"]
            };
        }
    }
}
