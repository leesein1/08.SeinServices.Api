using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/rcvhome-close")]
    /// <summary>
    /// RcvhomeCloseController 관련 기능을 제공합니다.
    /// </summary>
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

        [HttpGet("run-once")]
        [ProducesResponseType(typeof(CloseRunResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// RunOnce 작업을 수행합니다.
        /// </summary>
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

