using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SeinServices.Api.Services.DeskBuddy;

public sealed record SearchDocument(string Title, string Url, string Content);
public sealed record WebSearchResult(DateTimeOffset RetrievedAt, IReadOnlyList<SearchDocument> Documents);

public sealed class TavilyClient(HttpClient httpClient, IOptions<TavilyOptions> options, IMemoryCache cache)
{
    public async Task<WebSearchResult> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
            throw new DeskBuddyException(503, "TAVILY_NOT_CONFIGURED", "서버에 Tavily 검색 API 키를 설정해주세요.");

        var cacheKey = ("deskbuddy-search", query);
        if (cache.TryGetValue<WebSearchResult>(cacheKey, out var cached) && cached != null)
            return cached;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.Value.TimeoutSeconds, 1, 30)));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        request.Content = JsonContent.Create(new
        {
            query, search_depth = "basic", max_results = 3,
            include_answer = false, include_raw_content = false, auto_parameters = false
        });
        try
        {
            using var response = await httpClient.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw (int)response.StatusCode switch
                {
                    429 or 432 or 433 => new DeskBuddyException(429, "TAVILY_RATE_LIMITED", "검색 사용량 한도에 도달했습니다. 잠시 후 다시 시도해주세요."),
                    401 or 403 => new DeskBuddyException(502, "TAVILY_AUTH_FAILED", "서버의 Tavily API 키와 접근 권한을 확인해주세요."),
                    _ => new DeskBuddyException(502, "TAVILY_REQUEST_FAILED", "검색 요청에 실패했습니다.")
                };
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            if (json.RootElement.ValueKind != JsonValueKind.Object
                || !json.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                throw InvalidResponse();
            var documents = new List<SearchDocument>();
            foreach (var result in results.EnumerateArray().Take(3))
            {
                if (result.ValueKind != JsonValueKind.Object) continue;
                var url = StringValue(result, "url");
                var content = StringValue(result, "content");
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")
                    || string.IsNullOrWhiteSpace(content)) continue;
                var title = StringValue(result, "title");
                documents.Add(new SearchDocument(title[..Math.Min(title.Length, 200)],
                    url, content[..Math.Min(content.Length, 1800)]));
            }
            var search = new WebSearchResult(DateTimeOffset.UtcNow, documents);
            // Short-lived cache; failed and empty searches are not cached.
            if (documents.Count > 0) cache.Set(cacheKey, search, TimeSpan.FromMinutes(5));
            return search;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new DeskBuddyException(504, "TAVILY_TIMEOUT", "검색 응답 대기 시간이 초과되었습니다."); }
        catch (HttpRequestException)
        { throw new DeskBuddyException(502, "TAVILY_CONNECTION_FAILED", "검색 서비스에 연결할 수 없습니다."); }
        catch (JsonException) { throw InvalidResponse(); }
    }

    private static string StringValue(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
    private static DeskBuddyException InvalidResponse() => new(502, "TAVILY_INVALID_RESPONSE", "검색 서비스의 응답 형식이 올바르지 않습니다.");
}
