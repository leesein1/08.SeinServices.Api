using Microsoft.Data.SqlClient;

namespace SeinServices.Api.Data.FaultMon
{
    /// <summary>
    /// FaultMon DB 연결을 제공합니다.
    /// </summary>
    public class FaultMonDbHelper
    {
        private readonly string _connectionString;

        public FaultMonDbHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("FaultMonDb")
                ?? throw new InvalidOperationException("DB connection string 'FaultMonDb' is not configured.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
