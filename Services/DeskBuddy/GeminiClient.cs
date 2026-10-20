using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SeinServices.Api.Models.DeskBuddy.Responses;

namespace SeinServices.Api.Services.DeskBuddy;

public sealed record GeminiDecision(ChatResponseDto Answer, string? SearchQuery);

public sealed class GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options)
{
    public async Task<GeminiDecision> GenerateAsync(string message, string instruction, CancellationToken cancellationToken, bool allowSearch = false)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new DeskBuddyException(503, "GEMINI_NOT_CONFIGURED", "서버에 Gemini API 키를 설정해주세요.");
        }

        if (string.IsNullOrWhiteSpace(settings.Model)
            || settings.Model.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_' and not '.'))
        {
            throw new DeskBuddyException(503, "GEMINI_MODEL_INVALID", "서버의 Gemini 모델 설정을 확인해주세요.");
        }

        var properties = new Dictionary<string, object>
        {
            ["reply"] = new { type = "STRING", description = "한국어로 된 짧은 대답. 최대 600자." },
            ["emotion"] = new { type = "STRING", @enum = new[] { "neutral", "happy", "sad", "angry", "surprised" } }
        };
        if (allowSearch)
            properties["searchQuery"] = new { type = "STRING", description = "최신 외부 정보가 필요하면 검색어(최대 300자), 검색이 필요 없으면 빈 문자열." };

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = instruction } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = message } } } },
            generationConfig = new
            {
                maxOutputTokens = 1024,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties,
                    required = allowSearch ? new[] { "reply", "emotion", "searchQuery" } : new[] { "reply", "emotion" }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{settings.Model}:generateContent");
        request.Headers.Add("x-goog-api-key", settings.ApiKey);
        request.Content = JsonContent.Create(payload);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 1, 120)));

        try
        {
            using var response = await httpClient.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                // Do not return provider response bodies or credentials to callers.
                throw response.StatusCode switch
                {
                    HttpStatusCode.PaymentRequired => new DeskBuddyException(503, "GEMINI_BILLING_REQUIRED", "Gemini 프로젝트의 결제 상태 또는 선불 잔액을 확인해주세요. 무료 사용은 프로젝트의 무료 티어 설정을 확인해주세요."),
                    HttpStatusCode.TooManyRequests => new DeskBuddyException(429, "GEMINI_RATE_LIMITED", "Gemini 사용량 한도에 도달했습니다. 잠시 후 다시 시도해주세요."),
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new DeskBuddyException(502, "GEMINI_AUTH_FAILED", "서버의 Gemini API 키와 접근 권한을 확인해주세요."),
                    _ => new DeskBuddyException(502, "GEMINI_REQUEST_FAILED", "Gemini 요청에 실패했습니다. 서버의 모델 설정 또는 제공자 상태를 확인해주세요.")
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            return ParseResponse(document.RootElement, allowSearch);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DeskBuddyException(504, "GEMINI_TIMEOUT", "Gemini 응답 대기 시간이 초과되었습니다.");
        }
        catch (HttpRequestException)
        {
            throw new DeskBuddyException(502, "GEMINI_CONNECTION_FAILED", "Gemini에 연결할 수 없습니다.");
        }
        catch (JsonException)
        {
            throw InvalidResponse();
        }
    }

    private static GeminiDecision ParseResponse(JsonElement root, bool allowSearch)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw InvalidResponse();

        if (root.TryGetProperty("promptFeedback", out var feedback)
            && feedback.ValueKind == JsonValueKind.Object
            && feedback.TryGetProperty("blockReason", out _))
            throw new DeskBuddyException(422, "GEMINI_RESPONSE_BLOCKED", "이 요청에는 답변을 생성할 수 없습니다.");

        if (!root.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
            throw InvalidResponse();

        var candidate = candidates[0];
        if (candidate.ValueKind != JsonValueKind.Object)
            throw InvalidResponse();

        if (candidate.TryGetProperty("finishReason", out var finish))
        {
            var reason = finish.ValueKind == JsonValueKind.String ? finish.GetString() : null;
            if (reason is "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT" or "SPII" or "IMAGE_SAFETY")
                throw new DeskBuddyException(422, "GEMINI_RESPONSE_BLOCKED", "이 요청에는 답변을 생성할 수 없습니다.");
            if (reason != "STOP")
                throw InvalidResponse();
        }

        if (!candidate.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Object
            || !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
            throw InvalidResponse();

        var text = string.Concat(parts.EnumerateArray()
            .Where(part => part.ValueKind == JsonValueKind.Object
                && !(part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True)
                && part.TryGetProperty("text", out var value) && value.ValueKind == JsonValueKind.String)
            .Select(part => part.GetProperty("text").GetString()));

        using var answer = JsonDocument.Parse(text);
        var result = answer.RootElement;
        if (result.ValueKind != JsonValueKind.Object
            || !result.TryGetProperty("reply", out var replyValue) || replyValue.ValueKind != JsonValueKind.String
            || !result.TryGetProperty("emotion", out var emotionValue) || emotionValue.ValueKind != JsonValueKind.String)
            throw InvalidResponse();

        var reply = replyValue.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(reply) || reply.Length > 600)
            throw InvalidResponse();

        var emotion = emotionValue.GetString()?.Trim().ToLowerInvariant();
        if (emotion is not ("neutral" or "happy" or "sad" or "angry" or "surprised"))
            emotion = "neutral";

        string? query = null;
        if (allowSearch)
        {
            if (!result.TryGetProperty("searchQuery", out var queryValue) || queryValue.ValueKind != JsonValueKind.String)
                throw InvalidResponse();
            query = queryValue.GetString()?.Trim();
            if (query?.Length > 300) throw InvalidResponse();
        }
        return new GeminiDecision(new ChatResponseDto(reply, emotion), string.IsNullOrWhiteSpace(query) ? null : query);
    }

    private static DeskBuddyException InvalidResponse() =>
        new(502, "GEMINI_INVALID_RESPONSE", "Gemini에서 올바른 대사와 표정 응답을 받지 못했습니다.");
}
