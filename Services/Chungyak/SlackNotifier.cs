using System.Text;
using System.Text.Json;

namespace SeinServices.Api.Services.Chungyak
{
    /// <summary>
    /// Slack Webhook 기반 ?�림 ?�송 구현체입?�다.
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
        /// Slack Webhook?�로 메시지�??�송?�니??
        /// </summary>
        /// <param name="message">?�송???�스??메시지</param>
        /// <param name="cancellationToken">취소 ?�큰</param>
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

