using System.Net;
using System.Net.WebSockets;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeinServices.Api.Services.DeskBuddy;
using Xunit;

namespace SeinServices.Api.Tests;

public sealed class DeskBuddyTests
{
    private static string ProviderAnswer(string reply = "안녕!", string emotion = "happy", string finish = "STOP", string searchQuery = "") =>
        JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = finish,
            content = new { parts = new[] { new { text = JsonSerializer.Serialize(new { reply, emotion, searchQuery }) } } } } } });

    [Fact]
    public async Task Success_UsesStructuredOutputAndReturnsSmallDeviceContract()
    {
        using var app = new TestApi();
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "  안녕  " });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("안녕!", result.RootElement.GetProperty("reply").GetString());
        Assert.Equal("happy", result.RootElement.GetProperty("emotion").GetString());
        Assert.False(result.RootElement.GetProperty("searchUsed").GetBoolean());
        Assert.Empty(result.RootElement.GetProperty("sources").EnumerateArray());
        Assert.Equal(0, app.SearchCalls);
        using var sent = JsonDocument.Parse(app.OutgoingBody!);
        Assert.Equal("안녕", sent.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("application/json", sent.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
        Assert.Contains("테스트친구", sent.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Contains("responseSchema", app.OutgoingBody!);
        Assert.DoesNotContain("key", app.OutgoingUri!.Query);
        Assert.Equal("test-gemini-key", app.OutgoingKey);
        Assert.Contains("gemini-3.5-flash-lite", app.OutgoingUri.AbsolutePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task Unauthorized_DoesNotCallGemini(string? key)
    {
        using var app = new TestApi();
        using var client = app.CreateClient();
        if (key != null) client.DefaultRequestHeaders.Add("X-Device-Key", key);
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, app.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidMessage_IsRejectedBeforeGemini(string message)
    {
        using var app = new TestApi();
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, app.Calls);
    }

    [Fact]
    public async Task OversizedMessage_IsRejectedBeforeGemini()
    {
        using var app = new TestApi();
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = new string('a', 2001) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, app.Calls);
    }

    [Theory]
    [InlineData("Gemini:ApiKey")]
    [InlineData("DeskBuddy:DeviceApiKey")]
    public async Task MissingKey_DoesNotPreventSwaggerOrExistingAppStartup(string setting)
    {
        using var app = new TestApi(setting);
        using var client = app.CreateDeviceClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/chungyak/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/faultmon/swagger.json")).StatusCode);
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, app.Calls);
    }

    [Fact]
    public async Task Swagger_GroupsDeskBuddySeparatelyAndDocumentsDeviceKey()
    {
        using var app = new TestApi();
        using var client = app.CreateClient();
        var json = await client.GetStringAsync("/swagger/deskbuddy/swagger.json");
        using var document = JsonDocument.Parse(json);
        Assert.Equal(2, document.RootElement.GetProperty("paths").EnumerateObject().Count());
        var operation = document.RootElement.GetProperty("paths").GetProperty("/api/deskbuddy/chat").GetProperty("post");
        Assert.Contains(operation.GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "X-Device-Key");
        var socketOperation = document.RootElement.GetProperty("paths").GetProperty("/api/deskbuddy/device/ws").GetProperty("get");
        Assert.Contains(socketOperation.GetProperty("parameters").EnumerateArray(), p =>
            p.GetProperty("name").GetString() == "X-Device-Key" && p.GetProperty("required").GetBoolean());
        Assert.Contains("429", json);
        Assert.DoesNotContain("/api/deskbuddy", await client.GetStringAsync("/swagger/chungyak/swagger.json"));
        Assert.Contains("/swagger/deskbuddy/swagger.json", await client.GetStringAsync("/swagger/index.html"));
    }

    [Fact]
    public async Task UnknownEmotion_FallsBackToNeutral()
    {
        using var app = new TestApi { ProviderBody = ProviderAnswer(emotion: "confused") };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"emotion\":\"neutral\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(429, "GEMINI_RATE_LIMITED")]
    [InlineData(403, "GEMINI_AUTH_FAILED")]
    [InlineData(500, "GEMINI_REQUEST_FAILED")]
    [InlineData(402, "GEMINI_BILLING_REQUIRED")]
    public async Task ProviderFailure_DoesNotExposeProviderBody(int status, string code)
    {
        using var app = new TestApi { ProviderStatus = (HttpStatusCode)status, ProviderBody = "secret-provider-body" };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(status == 429 ? 429 : status == 402 ? 503 : 502, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(code, body);
        Assert.DoesNotContain("secret-provider-body", body);
        Assert.DoesNotContain("test-gemini-key", body);
        Assert.Contains("traceId", body);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"candidates\":[]}")]
    [InlineData("{\"candidates\":[null]}")]
    [InlineData("{\"candidates\":[{\"finishReason\":\"MAX_TOKENS\"}]}")]
    public async Task InvalidProviderResponse_ReturnsBadGateway(string body)
    {
        using var app = new TestApi { ProviderBody = body };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("GEMINI_INVALID_RESPONSE", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BlockedPrompt_ReturnsExplicitError()
    {
        using var app = new TestApi { ProviderBody = "{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}" };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ProviderTimeout_ReturnsGatewayTimeout()
    {
        using var app = new TestApi { SimulateTimeout = true };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
    }

    [Fact]
    public async Task RateLimit_RejectsWithoutAdditionalProviderCall()
    {
        using var app = new TestApi();
        using var client = app.CreateDeviceClient();
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" })).StatusCode);
        var rejected = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(20, app.Calls);
        Assert.Contains("DESKBUDDY_RATE_LIMITED", await rejected.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_RoutesGeminiThenTavilyThenGeminiAndReturnsActualSources()
    {
        using var app = new TestApi();
        app.ProviderBodies.Enqueue(ProviderAnswer(searchQuery: "인천 오늘 날씨"));
        app.ProviderBodies.Enqueue(ProviderAnswer(reply: "검색 자료에서 인천 날씨를 확인했어."));
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "오늘 인천 날씨 어때?" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "gemini", "tavily", "gemini" }, app.CallOrder);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(result.RootElement.GetProperty("searchUsed").GetBoolean());
        Assert.Equal("https://weather.example.com", result.RootElement.GetProperty("sources")[0].GetProperty("url").GetString());
        Assert.Contains("searchEvidence", app.OutgoingBody!);
        Assert.Contains("weather.example.com", app.OutgoingBody!);
        using var finalRequest = JsonDocument.Parse(app.OutgoingBody!);
        Assert.False(finalRequest.RootElement.GetProperty("generationConfig").GetProperty("responseSchema")
            .GetProperty("properties").TryGetProperty("searchQuery", out _));
        using var searchRequest = JsonDocument.Parse(app.SearchBody!);
        Assert.Equal("basic", searchRequest.RootElement.GetProperty("search_depth").GetString());
        Assert.False(searchRequest.RootElement.GetProperty("auto_parameters").GetBoolean());
        Assert.Equal(3, searchRequest.RootElement.GetProperty("max_results").GetInt32());
        Assert.Equal("south korea", searchRequest.RootElement.GetProperty("country").GetString());
        Assert.Equal("ko", searchRequest.RootElement.GetProperty("language").GetString());
        Assert.Equal("general", searchRequest.RootElement.GetProperty("topic").GetString());
        Assert.Contains("2026-10-20 21:00:00 +09:00", searchRequest.RootElement.GetProperty("query").GetString());
        var instructions = finalRequest.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString();
        Assert.Contains("대한민국", instructions);
        Assert.Contains("Asia/Seoul", instructions);
        Assert.Contains("2026-10-20 21:00:00 +09:00", instructions);
    }

    [Fact]
    public async Task HistoricalSearch_PreservesTargetDateAndDoesNotFilterToToday()
    {
        using var app = new TestApi();
        app.ProviderBodies.Enqueue(ProviderAnswer(searchQuery: "대한민국 인천 2025-01-01 날씨"));
        app.ProviderBodies.Enqueue(ProviderAnswer(reply: "해당 날짜 자료를 확인했어."));
        using var client = app.CreateDeviceClient();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "2025년 1월 1일 인천 날씨 검색해줘" })).StatusCode);
        using var request = JsonDocument.Parse(app.SearchBody!);
        Assert.Contains("2025-01-01", request.RootElement.GetProperty("query").GetString());
        Assert.Contains("2026-10-20 21:00:00 +09:00", request.RootElement.GetProperty("query").GetString());
        Assert.False(request.RootElement.TryGetProperty("time_range", out _));
        Assert.False(request.RootElement.TryGetProperty("start_date", out _));
        Assert.False(request.RootElement.TryGetProperty("end_date", out _));
    }

    [Fact]
    public async Task KoreanMidnight_SeparatesRelativeDateSearchCache()
    {
        using var app = new TestApi();
        app.Clock.UtcNow = DateTimeOffset.Parse("2026-10-20T14:59:59Z");
        for (var i = 0; i < 2; i++)
        {
            app.ProviderBodies.Enqueue(ProviderAnswer(searchQuery: "대한민국 인천 오늘 날씨"));
            app.ProviderBodies.Enqueue(ProviderAnswer());
        }
        using var client = app.CreateDeviceClient();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "오늘 인천 날씨?" })).StatusCode);
        app.Clock.UtcNow = app.Clock.UtcNow.AddSeconds(2);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "오늘 인천 날씨?" })).StatusCode);
        Assert.Equal(2, app.SearchCalls);
        using var request = JsonDocument.Parse(app.SearchBody!);
        Assert.Contains("2026-10-21 00:00:01 +09:00", request.RootElement.GetProperty("query").GetString());
    }

    [Fact]
    public async Task RepeatedQuery_UsesCacheAndStillAnswersWithGemini()
    {
        using var app = new TestApi();
        for (var i = 0; i < 2; i++)
        {
            app.ProviderBodies.Enqueue(ProviderAnswer(searchQuery: "인천 날씨"));
            app.ProviderBodies.Enqueue(ProviderAnswer());
        }
        using var client = app.CreateDeviceClient();
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "인천 날씨?" })).StatusCode);
        Assert.Equal(1, app.SearchCalls);
        Assert.Equal(4, app.Calls);
    }

    [Theory]
    [InlineData(401, 502, "TAVILY_AUTH_FAILED")]
    [InlineData(432, 429, "TAVILY_RATE_LIMITED")]
    [InlineData(433, 429, "TAVILY_RATE_LIMITED")]
    [InlineData(500, 502, "TAVILY_REQUEST_FAILED")]
    public async Task SearchFailure_IsExplicitAndDoesNotGenerateMadeUpAnswer(int providerStatus, int expectedStatus, string code)
    {
        using var app = new TestApi { ProviderBody = ProviderAnswer(searchQuery: "인천 날씨"), SearchStatus = (HttpStatusCode)providerStatus,
            SearchResponse = "secret-search-provider-body" };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "인천 날씨?" });
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(code, body);
        Assert.DoesNotContain("secret-search-provider-body", body);
        Assert.Equal(1, app.Calls);
    }

    [Fact]
    public async Task MissingSearchKey_OnlySearchRequestsFail()
    {
        using var app = new TestApi("Tavily:ApiKey");
        using var client = app.CreateDeviceClient();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" })).StatusCode);
        app.ProviderBody = ProviderAnswer(searchQuery: "인천 날씨");
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "인천 날씨?" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("TAVILY_NOT_CONFIGURED", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, app.SearchCalls);
    }

    [Theory]
    [InlineData("{\"results\":[]}")]
    [InlineData("{\"results\":[{\"title\":\"bad\",\"url\":\"javascript:alert(1)\",\"content\":\"bad\"}]}")]
    public async Task EmptyOrUnsafeResults_DoNotProduceUnverifiedFacts(string searchResponse)
    {
        using var app = new TestApi { ProviderBody = ProviderAnswer(searchQuery: "인천 날씨"), SearchResponse = searchResponse };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "인천 날씨?" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, app.Calls);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(result.RootElement.GetProperty("searchUsed").GetBoolean());
        Assert.Empty(result.RootElement.GetProperty("sources").EnumerateArray());
    }

    [Fact]
    public async Task InvalidSearchResponse_ReturnsBadGateway()
    {
        using var app = new TestApi { ProviderBody = ProviderAnswer(searchQuery: "인천 날씨"), SearchResponse = "not-json" };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "인천 날씨?" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("TAVILY_INVALID_RESPONSE", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExcessiveSearchQuery_IsRejectedBeforeSearch()
    {
        using var app = new TestApi { ProviderBody = ProviderAnswer(searchQuery: new string('a', 301)) };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "검색해줘" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(0, app.SearchCalls);
    }

    [Fact]
    public async Task SecondGeminiFailure_DoesNotReturnPreSearchDraft()
    {
        using var app = new TestApi();
        app.ProviderBodies.Enqueue(ProviderAnswer(reply: "아직 확인 안 한 정보", searchQuery: "인천 날씨"));
        app.ProviderBodies.Enqueue("not-json");
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "인천 날씨?" });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(1, app.SearchCalls);
        Assert.Contains("GEMINI_INVALID_RESPONSE", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(null, 401)]
    [InlineData("wrong", 401)]
    [InlineData("test-device-key", 400)]
    public async Task SocketEndpoint_RequiresAuthenticationAndUpgrade(string? key, int expectedStatus)
    {
        using var app = new TestApi();
        using var client = app.CreateClient();
        if (key != null) client.DefaultRequestHeaders.Add("X-Device-Key", key);
        Assert.Equal(expectedStatus, (int)(await client.GetAsync("/api/deskbuddy/device/ws")).StatusCode);
    }

    [Fact]
    public async Task Chat_PushesSameAnswerToEveryConnectedDisplay()
    {
        using var app = new TestApi();
        using var client = app.CreateDeviceClient();
        var wsClient = app.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = request => request.Headers["X-Device-Key"] = "test-device-key";
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var first = await wsClient.ConnectAsync(new Uri("ws://localhost/api/deskbuddy/device/ws"), deadline.Token);
        using var second = await wsClient.ConnectAsync(new Uri("ws://localhost/api/deskbuddy/device/ws"), deadline.Token);
        Assert.Contains("device.ready", await ReceiveTextAsync(first, deadline.Token));
        Assert.Contains("device.ready", await ReceiveTextAsync(second, deadline.Token));
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" }, deadline.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var one = await ReceiveTextAsync(first, deadline.Token);
        var two = await ReceiveTextAsync(second, deadline.Token);
        Assert.Equal(one, two);
        using var pushed = JsonDocument.Parse(one);
        Assert.Equal("chat.answer", pushed.RootElement.GetProperty("type").GetString());
        Assert.Equal("안녕!", pushed.RootElement.GetProperty("reply").GetString());
        Assert.Equal("happy", pushed.RootElement.GetProperty("emotion").GetString());
        Assert.False(pushed.RootElement.GetProperty("searchUsed").GetBoolean());
        Assert.DoesNotContain("test-device-key", one);
        Assert.DoesNotContain("test-gemini-key", one);
        Assert.DoesNotContain("sources", one); // Keep the ESP32 event small.
    }

    [Fact]
    public async Task DisconnectedDisplay_DoesNotBreakChat()
    {
        using var app = new TestApi();
        using var client = app.CreateDeviceClient();
        var wsClient = app.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = request => request.Headers["X-Device-Key"] = "test-device-key";
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/api/deskbuddy/device/ws"), deadline.Token);
        await ReceiveTextAsync(socket, deadline.Token);
        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", deadline.Token);
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" }, deadline.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var output = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            output.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private sealed class TestApi(string? missingSetting = null) : WebApplicationFactory<Program>
    {
        public MutableClock Clock { get; } = new();
        public string ProviderBody { get; set; } = ProviderAnswer();
        public HttpStatusCode ProviderStatus { get; set; } = HttpStatusCode.OK;
        public bool SimulateTimeout { get; set; }
        public int Calls { get; private set; }
        public string? OutgoingBody { get; private set; }
        public Uri? OutgoingUri { get; private set; }
        public string? OutgoingKey { get; private set; }
        public Queue<string> ProviderBodies { get; } = new();
        public List<string> CallOrder { get; } = [];
        public int SearchCalls { get; private set; }
        public string? SearchBody { get; private set; }
        public HttpStatusCode SearchStatus { get; set; } = HttpStatusCode.OK;
        public string SearchResponse { get; set; } = """{"results":[{"title":"날씨 자료","url":"https://weather.example.com","content":"인천의 날짜별 날씨 자료"}]}""";

        public HttpClient CreateDeviceClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Device-Key", "test-device-key");
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Docker");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = missingSetting == "Gemini:ApiKey" ? "" : "test-gemini-key",
                ["Tavily:ApiKey"] = missingSetting == "Tavily:ApiKey" ? "" : "test-tavily-key",
                ["DeskBuddy:DeviceApiKey"] = missingSetting == "DeskBuddy:DeviceApiKey" ? "" : "test-device-key",
                ["DeskBuddy:CharacterName"] = "테스트친구",
                ["Schedulers:EnableInProcess"] = "false",
                ["FaultMon:EnableSignalRScheduler"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<TimeProvider>(Clock);
                services.AddHttpClient<GeminiClient>().ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(this));
                services.AddHttpClient<TavilyClient>().ConfigurePrimaryHttpMessageHandler(() => new FakeSearchHandler(this));
            });
        }

        private sealed class FakeHandler(TestApi app) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                app.Calls++;
                app.CallOrder.Add("gemini");
                app.OutgoingBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                app.OutgoingUri = request.RequestUri;
                app.OutgoingKey = request.Headers.GetValues("x-goog-api-key").Single();
                if (app.SimulateTimeout) throw new TaskCanceledException("Simulated provider timeout");
                return new HttpResponseMessage(app.ProviderStatus)
                {
                    Content = new StringContent(app.ProviderBodies.Count > 0 ? app.ProviderBodies.Dequeue() : app.ProviderBody, Encoding.UTF8, "application/json")
                };
            }
        }

        private sealed class FakeSearchHandler(TestApi app) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                app.SearchCalls++;
                app.CallOrder.Add("tavily");
                Assert.Equal("test-tavily-key", request.Headers.Authorization?.Parameter);
                app.SearchBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(app.SearchStatus) { Content = new StringContent(app.SearchResponse, Encoding.UTF8, "application/json") };
            }
        }
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
