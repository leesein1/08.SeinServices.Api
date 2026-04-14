namespace SeinServices.Api.Models.Chungyak.Responses
{
    public class ScheduleLogResponseDto
    {
        public long Idx { get; set; }

        public byte JobCode { get; set; }

        public string JobCodeName { get; set; } = string.Empty;

        public string JobDesc { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public string? ScheduleNote { get; set; }
    }
}
