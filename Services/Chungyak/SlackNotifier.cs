using System.Text;
using System.Text.Json;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// SlackNotifier 관련 기능을 제공합니다.
    /// </summary>
    public class SlackNotifier : ISlackNotifier
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SlackNotifier> _logger;

        public SlackNotifier(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SlackNotifier> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// SendAsync 작업을 수행합니다.
        /// </summary>
        public async Task SendAsync(string message, CancellationToken cancellationToken)
        {
            var webhookUrl = _configuration["SlackApi:BaseUrl"];
            if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var payload = JsonSerializer.Serialize(new { text = message });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsync(webhookUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Slack webhook failed with status {StatusCode}", response.StatusCode);
            }
        }
    }
}

