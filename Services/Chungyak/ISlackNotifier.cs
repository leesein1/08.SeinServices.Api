namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// ISlackNotifier 계약을 정의합니다.
    /// </summary>
    public interface ISlackNotifier
    {
        Task SendAsync(string message, CancellationToken cancellationToken);
    }
}

