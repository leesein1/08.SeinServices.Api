using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Requests;
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

        [HttpGet]
        [ProducesResponseType(typeof(List<ScheduleLogResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<ScheduleLogResponseDto>> GetScheduleLogs([FromQuery] ScheduleLogSearchRequestDto request)
        {
            if (!_scheduleLogService.IsValidDateRange(request.StartedFrom, request.StartedTo))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_DATE_RANGE",
                    "StartedFrom must be less than or equal to StartedTo."));
            }

            if (!_scheduleLogService.IsValidStatusFilter(request.Status))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_STATUS",
                    "Status must be one of: SUCCESS, FAILED."));
            }

            if (!_scheduleLogService.TryMapJobCode(request.JobCode, out _))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_JOB_CODE",
                    "JobCode must be one of: SYNC, CLOSE."));
            }

            try
            {
                var response = _scheduleLogService.GetScheduleLogs(request);
                return Ok(response);
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "SCHEDULE_LOG_QUERY_FAILED",
                        "An unexpected error occurred while retrieving schedule logs."));
            }
        }

        [HttpGet("last")]
        [ProducesResponseType(typeof(ScheduleLastResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// GetLast 작업을 수행합니다.
        /// </summary>
        public ActionResult<ScheduleLastResponseDto> GetLast([FromQuery] byte jobCode = 1)
        {
            if (!_scheduleLogService.TryMapJobCodeToType(jobCode, out _))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_JOB_CODE",
                    "jobCode must be 1(sync) or 2(close)."));
            }

            try
            {
                var response = _scheduleLogService.GetLastScheduleLogByJobCode(jobCode);

                if (response is null)
                {
                    return NotFound(CreateErrorResponse(
                        "SCHEDULE_LOG_NOT_FOUND",
                        "No schedule log found for the requested job code."));
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
