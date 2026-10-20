using Microsoft.Extensions.Options;
using SeinServices.Api.Models.DeskBuddy.Responses;

namespace SeinServices.Api.Services.DeskBuddy;

public sealed class DeskBuddyService(GeminiClient gemini, IOptions<DeskBuddyOptions> options)
{
    public Task<ChatResponseDto> ChatAsync(string message, CancellationToken cancellationToken)
    {
        var character = options.Value;
        var instruction = $"""
            너의 이름은 {character.CharacterName}이다. 사용자의 책상 위에 있는 작은 친구 역할이다.
            성격과 말투: {character.Personality}
            기본적으로 한국어로 1~3문장, 최대 600자로 짧게 대답한다.
            스피커로 읽을 문장이므로 마크다운, 표, 이모지, 복잡한 기호를 쓰지 않는다.
            실제 기기 제어나 메모 저장 기능은 없으므로 실행하거나 저장했다고 주장하지 않는다.
            reply에는 대사, emotion에는 neutral, happy, sad, angry, surprised 중 하나를 지정한다.
            """;
        return gemini.GenerateAsync(message.Trim(), instruction, cancellationToken);
    }
}
