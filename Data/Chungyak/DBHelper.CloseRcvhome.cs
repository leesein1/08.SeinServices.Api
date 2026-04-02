namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        /// <summary>
        /// CloseRcvhome 작업을 수행합니다.
        /// </summary>
        public int CloseRcvhome()
        {
            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = $@"
                UPDATE dbo.TB_RCVHOME
                SET CLS_YN = 'Y'
                WHERE END_DE < {KstTodaySql}
                  AND (CLS_YN IS NULL OR CLS_YN <> 'Y');

                SELECT @@ROWCOUNT;";

            conn.Open();
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }
    }
}

