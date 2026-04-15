namespace SeinServices.Api.Models.Chungyak.Requests
{
    public class AlarmLogSearchRequestDto
    {
        public DateTime? SendFrom { get; set; }

        public DateTime? SendTo { get; set; }

        public string? SendStatus { get; set; }

        public string? AlarmType { get; set; }

        public string? AlarmSource { get; set; }

        public string? PblancId { get; set; }
    }
}
