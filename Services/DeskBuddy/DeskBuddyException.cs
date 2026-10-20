namespace SeinServices.Api.Services.DeskBuddy;

public sealed class DeskBuddyException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
