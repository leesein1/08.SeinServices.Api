using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    /// <summary>
    /// 모집공고 마감 배치 수동 실행 API를 제공합니다.
    /// </summary>
    [ApiController]
    [Route("api/rcvhome-close")]
    public class RcvhomeCloseController : SeinServices.Api.Controllers.BaseController
    {
        private readonly RcvhomeCloseService _rcvhomeCloseService;
        private readonly IConfiguration _configuration;

        public RcvhomeCloseController(
            RcvhomeCloseService rcvhomeCloseService,
            IConfiguration configuration)
        {
            _rcvhomeCloseService = rcvhomeCloseService;
            _configuration = configuration;
        }

        /// <summary>
        /// 모집공고 마감 배치를 수동으로 즉시 1회 실행합니다.
        /// </summary>
        [HttpGet("run-once")]
        [ProducesResponseType(typeof(CloseRunResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CloseRunResponseDto>> RunOnce(CancellationToken cancellationToken)
        {
            if (!TryAuthorizeJobRequest(_configuration, out var unauthorizedResult))
            {
                return unauthorizedResult!;
            }

            try
            {
                var result = await _rcvhomeCloseService.RunOnceAsync(cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "CLOSE_CANCELLED",
                        "Close job was cancelled."));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "CLOSE_RUN_FAILED",
                        "An unexpected error occurred while running close job."));
            }
        }
    }
}

