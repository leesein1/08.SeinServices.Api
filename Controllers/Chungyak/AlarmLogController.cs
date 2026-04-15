using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/alarm-log")]
    /// <summary>
    /// AlarmLogController 관련 기능을 제공합니다.
    /// </summary>
    public class AlarmLogController : SeinServices.Api.Controllers.BaseController
    {
        private readonly AlarmLogService _alarmLogService;

        public AlarmLogController(AlarmLogService alarmLogService)
        {
            _alarmLogService = alarmLogService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AlarmLogResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<AlarmLogResponseDto>> GetAlarmLogs([FromQuery] AlarmLogSearchRequestDto request)
        {
            if (!_alarmLogService.IsValidDateRange(request.SendFrom, request.SendTo))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_DATE_RANGE",
                    "SendFrom must be less than or equal to SendTo."));
            }

            if (!_alarmLogService.IsValidSendStatus(request.SendStatus))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_SEND_STATUS",
                    "SendStatus must be one of: SUCCESS, FAIL, SKIPPED."));
            }

            try
            {
                var response = _alarmLogService.GetAlarmLogs(request);
                return Ok(response);
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "ALARM_LOG_QUERY_FAILED",
                        "An unexpected error occurred while retrieving alarm logs."));
            }
        }
    }
}
