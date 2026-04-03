namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// ScheduleLastResponseDto 관련 기능을 제공합니다.
    /// </summary>
    public class ScheduleLastResponseDto
    {
        public int JobType { get; set; } // 1: main(sync), 0: close

        public byte JobCode { get; set; } // 1: sync, 2: close

        public string Status { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public DateTime LastCommunicatedAt { get; set; }

        public string? ScheduleNote { get; set; }
    }
}
