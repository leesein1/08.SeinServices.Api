namespace SeinServices.Api.Models.Chungyak.Responses
{
    /// <summary>
    /// CloseRunResponseDto 관련 기능을 제공합니다.
    /// </summary>
    public class CloseRunResponseDto
    {
        public bool Success { get; set; }

        public bool Skipped { get; set; }

        public string Message { get; set; } = string.Empty;

        public int ClosedCount { get; set; }
    }
}

