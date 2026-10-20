using System.Net;
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
    private static string ProviderAnswer(string reply = "안녕!", string emotion = "happy", string finish = "STOP") =>
        JsonSerializer.Serialize(new { candidates = new[] { new { finishReason = finish,
            content = new { parts = new[] { new { text = JsonSerializer.Serialize(new { reply, emotion }) } } } } } });

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
        Assert.Equal(2, result.RootElement.EnumerateObject().Count());
        using var sent = JsonDocument.Parse(app.OutgoingBody!);
        Assert.Equal("안녕", sent.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("application/json", sent.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
        Assert.Contains("테스트친구", sent.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Contains("responseSchema", app.OutgoingBody!);
        Assert.DoesNotContain("key", app.OutgoingUri!.Query);
        Assert.Equal("test-gemini-key", app.OutgoingKey);
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
        Assert.Single(document.RootElement.GetProperty("paths").EnumerateObject());
        var operation = document.RootElement.GetProperty("paths").GetProperty("/api/deskbuddy/chat").GetProperty("post");
        Assert.Contains(operation.GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "X-Device-Key");
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
    public async Task ProviderFailure_DoesNotExposeProviderBody(int status, string code)
    {
        using var app = new TestApi { ProviderStatus = (HttpStatusCode)status, ProviderBody = "secret-provider-body" };
        using var client = app.CreateDeviceClient();
        var response = await client.PostAsJsonAsync("/api/deskbuddy/chat", new { message = "안녕" });
        Assert.Equal(status == 429 ? 429 : 502, (int)response.StatusCode);
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

    private sealed class TestApi(string? missingSetting = null) : WebApplicationFactory<Program>
    {
        public string ProviderBody { get; set; } = ProviderAnswer();
        public HttpStatusCode ProviderStatus { get; set; } = HttpStatusCode.OK;
        public bool SimulateTimeout { get; set; }
        public int Calls { get; private set; }
        public string? OutgoingBody { get; private set; }
        public Uri? OutgoingUri { get; private set; }
        public string? OutgoingKey { get; private set; }

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
                ["DeskBuddy:DeviceApiKey"] = missingSetting == "DeskBuddy:DeviceApiKey" ? "" : "test-device-key",
                ["DeskBuddy:CharacterName"] = "테스트친구",
                ["Schedulers:EnableInProcess"] = "false",
                ["FaultMon:EnableSignalRScheduler"] = "false"
            }));
            builder.ConfigureServices(services => services.AddHttpClient<GeminiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(this)));
        }

        private sealed class FakeHandler(TestApi app) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                app.Calls++;
                app.OutgoingBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                app.OutgoingUri = request.RequestUri;
                app.OutgoingKey = request.Headers.GetValues("x-goog-api-key").Single();
                if (app.SimulateTimeout) throw new TaskCanceledException("Simulated provider timeout");
                return new HttpResponseMessage(app.ProviderStatus)
                {
                    Content = new StringContent(app.ProviderBody, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
