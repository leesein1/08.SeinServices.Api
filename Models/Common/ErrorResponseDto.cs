namespace SeinServices.Api.Models.Common
{
    /// <summary>
    /// API 에러 응답 정보를 담는 공통 DTO입니다.
    /// </summary>
    public class ErrorResponseDto
    {
        /// <summary>에러 식별 코드</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>에러 메시지</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>요청 추적용 식별자</summary>
        public string? TraceId { get; set; }
    }
}

