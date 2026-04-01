using Microsoft.Data.SqlClient;

namespace SeinServices.Api.Data.Chungyak
{
    /// <summary>
    /// DBHelper 관련 기능을 제공합니다.
    /// </summary>
    public partial class DBHelper
    {
        private readonly string _connectionString;

        public DBHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ChungyakDb")
                ?? throw new InvalidOperationException("DB connection string 'ChungyakDb' is not configured.");
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

