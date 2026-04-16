namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// SubscribeAlarmDispatchResponseDto 관련 기능을 제공합니다.
    /// </summary>
    public class SubscribeAlarmDispatchResponseDto
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ClaimedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public int SkippedCount { get; set; }
    }
}

