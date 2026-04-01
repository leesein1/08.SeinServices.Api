namespace SeinServices.Api.Models.Common
{
    /// <summary>
    /// ErrorResponseDto 관련 기능을 제공합니다.
    /// </summary>
    public class ErrorResponseDto
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? TraceId { get; set; }
    }
}

