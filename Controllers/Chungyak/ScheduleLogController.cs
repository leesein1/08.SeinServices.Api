using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/schedule-log")]
    /// <summary>
    /// ScheduleLogController 관련 기능을 제공합니다.
    /// </summary>
    public class ScheduleLogController : SeinServices.Api.Controllers.BaseController
    {
        private readonly ScheduleLogService _scheduleLogService;

        public ScheduleLogController(ScheduleLogService scheduleLogService)
        {
            _scheduleLogService = scheduleLogService;
        }

        [HttpGet("last")]
        [ProducesResponseType(typeof(ScheduleLastResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// GetLast 작업을 수행합니다.
        /// </summary>
        public ActionResult<ScheduleLastResponseDto> GetLast([FromQuery] int jobType)
        {
            if (!_scheduleLogService.TryMapJobTypeToCode(jobType, out _))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_JOB_TYPE",
                    "jobType must be 1(main sync) or 0(close)."));
            }

            try
            {
                var response = _scheduleLogService.GetLastScheduleLog(jobType);
                if (response is null)
                {
                    return NotFound(CreateErrorResponse(
                        "SCHEDULE_LOG_NOT_FOUND",
                        "No schedule log found for the requested job type."));
                }

                return Ok(response);
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "SCHEDULE_LOG_QUERY_FAILED",
                        "An unexpected error occurred while retrieving schedule log."));
            }
        }
    }
}
