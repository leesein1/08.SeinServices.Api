using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Models.DeskBuddy.Requests;
using SeinServices.Api.Models.DeskBuddy.Responses;
using SeinServices.Api.Services.DeskBuddy;

namespace SeinServices.Api.Controllers.DeskBuddy;

[ApiController]
[Route("api/deskbuddy")]
public sealed class DeskBuddyController(DeskBuddyService service, IOptions<DeskBuddyOptions> options,
    DeskBuddyDeviceConnections devices) : BaseController
{
    [HttpPost("chat")]
    [EnableRateLimiting("deskbuddy")]
    [RequestSizeLimit(16 * 1024)]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ChatResponseDto>> Chat(
        [FromBody] ChatRequestDto request,
        [FromHeader(Name = "X-Device-Key")] string? deviceKey,
        CancellationToken cancellationToken)
    {
        var configuredKey = options.Value.DeviceApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
            return StatusCode(503, CreateErrorResponse("DESKBUDDY_NOT_CONFIGURED", "서버에 DeskBuddy 장치 접속 키를 설정해주세요."));

        if (string.IsNullOrEmpty(deviceKey) || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey)),
                SHA256.HashData(Encoding.UTF8.GetBytes(deviceKey))))
            return Unauthorized(CreateErrorResponse("DESKBUDDY_UNAUTHORIZED", "장치 접속 키가 올바르지 않습니다."));

        try
        {
            var answer = await service.ChatAsync(request.Message, cancellationToken);
            devices.Publish(answer);
            return Ok(answer);
        }
        catch (DeskBuddyException error)
        {
            return StatusCode(error.StatusCode, CreateErrorResponse(error.Code, error.Message));
        }
    }

    [HttpGet("device/ws")]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeviceSocket([FromHeader(Name = "X-Device-Key")] string? deviceKey,
        CancellationToken cancellationToken)
    {
        var configuredKey = options.Value.DeviceApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
            return StatusCode(503, CreateErrorResponse("DESKBUDDY_NOT_CONFIGURED", "서버에 DeskBuddy 장치 접속 키를 설정해주세요."));
        if (string.IsNullOrEmpty(deviceKey) || !CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey)), SHA256.HashData(Encoding.UTF8.GetBytes(deviceKey))))
            return Unauthorized(CreateErrorResponse("DESKBUDDY_UNAUTHORIZED", "장치 접속 키가 올바르지 않습니다."));
        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return BadRequest(CreateErrorResponse("DESKBUDDY_WEBSOCKET_REQUIRED", "WebSocket 클라이언트로 연결해주세요."));
        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await devices.RunAsync(socket, cancellationToken);
        return new EmptyResult();
    }
}
