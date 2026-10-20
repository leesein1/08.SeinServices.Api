namespace SeinServices.Api.Services.DeskBuddy;

public sealed class TavilyOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}
