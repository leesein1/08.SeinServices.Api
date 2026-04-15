namespace SeinServices.Api.Models.Chungyak.Responses
{
    public class AlarmLogResponseDto
    {
        public long Idx { get; set; }

        public string AlarmSource { get; set; } = string.Empty;

        public int? SubscribeIdx { get; set; }

        public string PblancId { get; set; } = string.Empty;

        public string AlarmType { get; set; } = string.Empty;

        public DateTime? TargetDate { get; set; }

        public string SendStatus { get; set; } = string.Empty;

        public DateTime SendTime { get; set; }

        public string? AlarmTitle { get; set; }

        public string? AlarmMessage { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
