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

            if (isFavorite)
            {
                const string existsSql = @"SELECT COUNT(1) FROM dbo.TB_SUBSCRIBE WHERE PBLANC_ID = @PBLANC_ID;";
                using (var existsCmd = new SqlCommand(existsSql, conn))
                {
                    existsCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);
                    var exists = Convert.ToInt32(existsCmd.ExecuteScalar());
                    if (exists > 0)
                    {
                        return FavoriteResult.AlreadyAdded;
                    }
                }

                const string insertSql = @"
                    INSERT INTO dbo.TB_SUBSCRIBE (PBLANC_ID, ALERT_DATE, CREATE_TIME, BRTC_NM, SIGNGU_NM)
                    VALUES (@PBLANC_ID, GETDATE(), GETDATE(), @BRTC_NM, @SIGNGU_NM);";
                using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);
                insertCmd.Parameters.AddWithValue("@BRTC_NM", brtcNm);
                insertCmd.Parameters.AddWithValue("@SIGNGU_NM", signguNm);

                var affected = insertCmd.ExecuteNonQuery();
                return affected > 0 ? FavoriteResult.Added : FavoriteResult.None;
            }

            const string deleteSql = @"DELETE FROM dbo.TB_SUBSCRIBE WHERE PBLANC_ID = @PBLANC_ID;";
            using var deleteCmd = new SqlCommand(deleteSql, conn);
            deleteCmd.Parameters.AddWithValue("@PBLANC_ID", pblancId);

            var deleted = deleteCmd.ExecuteNonQuery();
            return deleted > 0 ? FavoriteResult.Removed : FavoriteResult.AlreadyRemoved;
        }
    }
}

