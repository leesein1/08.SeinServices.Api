namespace SeinServices.Api.Models.Chungyak.Internal
{
    /// <summary>
    /// SubscribeAlarmDispatchItem 관련 기능을 제공합니다.
    /// </summary>
    public class SubscribeAlarmDispatchItem
    {
        public long Idx { get; set; }
        public int SubscribeIdx { get; set; }
        public string PblancId { get; set; } = string.Empty;
        public string AlarmType { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
        public string? AlarmTitle { get; set; }
        public string? AlarmMessage { get; set; }
        public DateTime SendAt { get; set; }
        public int RetryCount { get; set; }
        public string? Status { get; set; }
    }
}

