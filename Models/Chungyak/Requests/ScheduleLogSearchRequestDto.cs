namespace SeinServices.Api.Models.Chungyak.Requests
{
    public class ScheduleLogSearchRequestDto
    {
        public DateTime? StartedFrom { get; set; }

        public DateTime? StartedTo { get; set; }

        public string? Status { get; set; }

        public string? JobCode { get; set; }
    }
}
