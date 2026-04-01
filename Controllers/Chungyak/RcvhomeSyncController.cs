using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    /// <summary>
    /// 모집공고 동기화 수동 실행 API를 제공합니다.
    /// </summary>
    [ApiController]
    [Route("api/rcvhome-sync")]
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

        /// <summary>
        /// 모집공고 동기화를 수동으로 즉시 1회 실행합니다.
        /// </summary>
        [HttpGet("run-once")]
        [ProducesResponseType(typeof(SyncRunResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
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

