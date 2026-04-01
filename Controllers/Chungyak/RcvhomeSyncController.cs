using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/rcvhome-sync")]
    /// <summary>
    /// RcvhomeSyncController 관련 기능을 제공합니다.
    /// </summary>
    public class RcvhomeSyncController : SeinServices.Api.Controllers.BaseController
    {
        private readonly RecruitSyncService _recruitSyncService;
        private readonly IConfiguration _configuration;

        public RcvhomeSyncController(
            RecruitSyncService recruitSyncService,
            IConfiguration configuration)
        {
            _recruitSyncService = recruitSyncService;
            _configuration = configuration;
        }

        [HttpGet("run-once")]
        [ProducesResponseType(typeof(SyncRunResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// RunOnce 작업을 수행합니다.
        /// </summary>
        public async Task<ActionResult<SyncRunResponseDto>> RunOnce(CancellationToken cancellationToken)
        {
            if (!TryAuthorizeJobRequest(_configuration, out var unauthorizedResult))
            {
                return unauthorizedResult!;
            }

            try
            {
                var result = await _recruitSyncService.RunOnceAsync(cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "SYNC_CANCELLED",
                        "Sync was cancelled."));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "SYNC_RUN_FAILED",
                        "An unexpected error occurred while running sync."));
            }
        }
    }
}

