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
        public async Task<SlackSendResult> SendAsync(string message, CancellationToken cancellationToken)
        {
            var webhookUrl = _configuration["SlackApi:BaseUrl"];
            if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(message))
            {
                return new SlackSendResult
                {
                    IsSuccess = false,
                    SendStatus = "SKIPPED",
                    ErrorMessage = "Slack webhook URL or message is empty."
                };
            }

            try
            {
                var payload = JsonSerializer.Serialize(new { text = message });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                using var response = await client.PostAsync(webhookUrl, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Slack webhook failed with status {(int)response.StatusCode} ({response.StatusCode}).";
                    _logger.LogWarning(errorMessage);
                    return new SlackSendResult
                    {
                        IsSuccess = false,
                        SendStatus = "FAIL",
                        ErrorMessage = errorMessage
                    };
                }

                return new SlackSendResult
                {
                    IsSuccess = true,
                    SendStatus = "SUCCESS"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Slack webhook call failed.");
                return new SlackSendResult
                {
                    IsSuccess = false,
                    SendStatus = "FAIL",
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}

