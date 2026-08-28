namespace SeinServices.Api.Models.FaultMon.Requests
{
    /// <summary>
    /// FaultMon 누적 고장 이력 검색 조건입니다.
    /// </summary>
    public class FaultHistorySearchRequestDto
    {
        public string? Keyword { get; set; }
        public string? ReceiptNo { get; set; }
        public string? VehicleNo { get; set; }
        public string? CustomerName { get; set; }
        public string? MangerName { get; set; }
        public string? Statuses { get; set; }
        public DateTime? SetTimeFrom { get; set; }
        public DateTime? SetTimeTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }
}
