using System.Data;
using Microsoft.Data.SqlClient;
using SeinServices.Api.Models.FaultMon.Requests;

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

        public DataTable SearchFaultHistory(FaultHistorySearchRequestDto request)
        {
            return ExecuteStoredProcedure(
                "[dbo].[PROC_FAULT_HISTORY_SEARCH]",
                cmd =>
                {
                    AddNullableString(cmd, "@Keyword", request.Keyword, 200);
                    AddNullableString(cmd, "@ReceiptNo", request.ReceiptNo, 50);
                    AddNullableString(cmd, "@VehicleNo", request.VehicleNo, 50);
                    AddNullableString(cmd, "@CustomerName", request.CustomerName, 100);
                    AddNullableString(cmd, "@MangerName", request.MangerName, 100);
                    AddNullableString(cmd, "@Statuses", request.Statuses, 50);
                    AddNullableDateTime(cmd, "@SetTimeFrom", request.SetTimeFrom);
                    AddNullableDateTime(cmd, "@SetTimeTo", request.SetTimeTo);
                    cmd.Parameters.Add("@Page", SqlDbType.Int).Value = Math.Max(1, request.Page);
                    cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = Math.Clamp(request.PageSize, 1, 500);
                });
        }

        public int ExecuteScheduleRepeatInsert()
        {
            return ExecuteNonQuery("[dbo].[PROC_SCH_REPEAT_INSERT]");
        }

        private static void AddNullableString(SqlCommand cmd, string name, string? value, int size)
        {
            var parameter = cmd.Parameters.Add(name, SqlDbType.NVarChar, size);
            parameter.Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static void AddNullableDateTime(SqlCommand cmd, string name, DateTime? value)
        {
            var parameter = cmd.Parameters.Add(name, SqlDbType.DateTime2);
            parameter.Value = value.HasValue ? value.Value : DBNull.Value;
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
