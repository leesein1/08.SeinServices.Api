namespace SeinServices.Api.Services.DeskBuddy;

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public int TimeoutSeconds { get; set; } = 30;
}
