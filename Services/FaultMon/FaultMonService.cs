using System.Data;
using SeinServices.Api.Data.FaultMon;

namespace SeinServices.Api.Services.FaultMon
{
    /// <summary>
    /// FaultMon 조회 서비스를 제공합니다.
    /// </summary>
    public class FaultMonService
    {
        private readonly FaultMonRepository _repository;

        public FaultMonService(FaultMonRepository repository)
        {
            _repository = repository;
        }

        public List<Dictionary<string, object?>> GetFaultList()
        {
            return ToRows(_repository.GetFaultList());
        }

        public List<Dictionary<string, object?>> GetStatToday()
        {
            return ToRows(_repository.GetStatToday());
        }

        public List<Dictionary<string, object?>> GetFaultListDetail(int incidentId)
        {
            return ToRows(_repository.GetFaultListDetail(incidentId));
        }

        public List<Dictionary<string, object?>> GetFaultListDetailPop(int incidentId)
        {
            return ToRows(_repository.GetFaultListDetailPop(incidentId));
        }

        public int ExecuteScheduleRepeatInsert()
        {
            return _repository.ExecuteScheduleRepeatInsert();
        }

        private static List<Dictionary<string, object?>> ToRows(DataTable dataTable)
        {
            var rows = new List<Dictionary<string, object?>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var values = new Dictionary<string, object?>();

                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column];
                    values[column.ColumnName] = value == DBNull.Value ? null : value;
                }

                rows.Add(values);
            }

            return rows;
        }
    }
}
