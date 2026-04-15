namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// SlackSendResult 상태 값을 정의합니다.
    /// </summary>
    public sealed class SlackSendResult
    {
        public bool IsSuccess { get; init; }

        public string SendStatus { get; init; } = "FAIL";

        public string? ErrorMessage { get; init; }
    }
}
