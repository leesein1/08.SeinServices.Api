using System.Data;
using Microsoft.Data.SqlClient;

namespace SeinServices.Api.Data.FaultMon
{
    /// <summary>
    /// FaultMon 저장 프로시저 호출을 담당합니다.
    /// </summary>
    public class FaultMonRepository
    {
        private readonly FaultMonDbHelper _dbHelper;

        public FaultMonRepository(FaultMonDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public DataTable GetFaultList()
        {
            return ExecuteStoredProcedure("[dbo].[PROC_RECENT_FAULT_LIST]");
        }

        public DataTable GetStatToday()
        {
            return ExecuteStoredProcedure("[dbo].[PROC_FAULT_STATS_TODAY]");
        }

        public DataTable GetFaultListDetail(int incidentId)
        {
            return ExecuteStoredProcedure(
                "[dbo].[PROC_RECENT_FAULT_DETAIL]",
                cmd => cmd.Parameters.Add("@IncidentID", SqlDbType.Int).Value = incidentId);
        }

        public DataTable GetFaultListDetailPop(int incidentId)
        {
            return ExecuteStoredProcedure(
                "[dbo].[PROC_RECENT_FAULT_DETAIL_POP]",
                cmd => cmd.Parameters.Add("@IncidentID", SqlDbType.Int).Value = incidentId);
        }

        public int ExecuteScheduleRepeatInsert()
        {
            return ExecuteNonQuery("[dbo].[PROC_SCH_REPEAT_INSERT]");
        }

        private int ExecuteNonQuery(string procedureName, Action<SqlCommand>? configureCommand = null)
        {
            using var conn = _dbHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedureName;
            configureCommand?.Invoke(cmd);

            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        private DataTable ExecuteStoredProcedure(string procedureName, Action<SqlCommand>? configureCommand = null)
        {
            var dt = new DataTable();

            using var conn = _dbHelper.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedureName;
            configureCommand?.Invoke(cmd);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            dt.Load(reader);

            return dt;
        }
    }
}
