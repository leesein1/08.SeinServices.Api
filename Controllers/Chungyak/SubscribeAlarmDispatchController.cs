using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/subscribe-alarm-dispatch")]
    /// <summary>
    /// SubscribeAlarmDispatchController 관련 기능을 제공합니다.
    /// </summary>
    public class SubscribeAlarmDispatchController : SeinServices.Api.Controllers.BaseController
    {
        private readonly SubscribeAlarmDispatchService _dispatchService;
        private readonly IConfiguration _configuration;

        public SubscribeAlarmDispatchController(
            SubscribeAlarmDispatchService dispatchService,
            IConfiguration configuration)
        {
            _dispatchService = dispatchService;
            _configuration = configuration;
        }

        [HttpGet("run-once")]
        [ProducesResponseType(typeof(SubscribeAlarmDispatchResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// RunOnce 작업을 수행합니다.
        /// </summary>
        public async Task<ActionResult<SubscribeAlarmDispatchResponseDto>> RunOnce(CancellationToken cancellationToken)
        {
            if (!TryAuthorizeJobRequest(_configuration, out var unauthorizedResult))
            {
                return unauthorizedResult!;
            }

            try
            {
                var result = await _dispatchService.RunOnceAsync(cancellationToken);

                if (!result.Success && !result.Skipped)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        CreateErrorResponse(
                            "SUBSCRIBE_ALARM_DISPATCH_FAILED",
                            result.Message));
                }

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "SUBSCRIBE_ALARM_DISPATCH_CANCELLED",
                        "Subscribe alarm dispatch was cancelled."));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "SUBSCRIBE_ALARM_DISPATCH_FAILED",
                        "An unexpected error occurred while dispatching subscribe alarms."));
            }
        }
    }
}

