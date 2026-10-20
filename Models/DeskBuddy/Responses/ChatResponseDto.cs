namespace SeinServices.Api.Models.DeskBuddy.Responses;

public sealed record ChatResponseDto(string Reply, string Emotion)
{
    public bool SearchUsed { get; init; }
    public IReadOnlyList<SearchSourceDto> Sources { get; init; } = [];
}

public sealed record SearchSourceDto(string Title, string Url);
