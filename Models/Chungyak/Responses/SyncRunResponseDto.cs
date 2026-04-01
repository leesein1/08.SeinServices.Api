namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// SyncRunResponseDto 관련 기능을 제공합니다.
    /// </summary>
    public class SyncRunResponseDto
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int InsertCount { get; set; }
        public int UpdateCount { get; set; }
        public int NoneCount { get; set; }
        public int ErrorCount { get; set; }
    }
}

