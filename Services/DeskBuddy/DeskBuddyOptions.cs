namespace SeinServices.Api.Services.DeskBuddy;

public sealed class DeskBuddyOptions
{
    public string DeviceApiKey { get; set; } = string.Empty;
    public string CharacterName { get; set; } = "데스크버디";
    public string Personality { get; set; } = "호기심 많고 다정한 책상 위의 작은 친구. 친근한 반말을 사용한다.";
}
