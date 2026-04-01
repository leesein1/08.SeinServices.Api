using Microsoft.Data.SqlClient;

namespace SeinServices.Api.Data.Chungyak
{
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

