using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Models.FaultMon.Requests;
using SeinServices.Api.Services.FaultMon;

namespace SeinServices.Api.Controllers.FaultMon
{
    [ApiController]
    [Route("api/faultmon")]
    /// <summary>
    /// FaultMon 고장 관제 API를 제공합니다.
    /// </summary>
    public class FaultMonController : SeinServices.Api.Controllers.BaseController
    {
        private readonly FaultMonService _faultMonService;

        public FaultMonController(FaultMonService faultMonService)
        {
            _faultMonService = faultMonService;
        }

        [HttpGet("faults")]
        [HttpGet("/Fault/GetFaultList")]
        [ProducesResponseType(typeof(List<Dictionary<string, object?>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<Dictionary<string, object?>>> GetFaultList()
        {
            return ExecuteFaultMonQuery(_faultMonService.GetFaultList, "FAULTMON_LIST_QUERY_FAILED");
        }

        [HttpGet("stats/today")]
        [HttpGet("/Fault/GetStatToday")]
        [ProducesResponseType(typeof(List<Dictionary<string, object?>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<Dictionary<string, object?>>> GetStatToday()
        {
            return ExecuteFaultMonQuery(_faultMonService.GetStatToday, "FAULTMON_STATS_QUERY_FAILED");
        }

        [HttpGet("faults/search")]
        [ProducesResponseType(typeof(List<Dictionary<string, object?>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<Dictionary<string, object?>>> SearchFaultHistory([FromQuery] FaultHistorySearchRequestDto request)
        {
            return ExecuteFaultMonQuery(
                () => _faultMonService.SearchFaultHistory(request),
                "FAULTMON_HISTORY_SEARCH_QUERY_FAILED");
        }

        [HttpGet("faults/{incidentId:int}")]
        [HttpGet("/Fault/GetFaultListDetail")]
        [ProducesResponseType(typeof(List<Dictionary<string, object?>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<Dictionary<string, object?>>> GetFaultListDetail([FromQuery(Name = "IncidentID")] int? legacyIncidentId, int? incidentId = null)
        {
            var selectedIncidentId = incidentId ?? legacyIncidentId;
            if (!selectedIncidentId.HasValue || selectedIncidentId.Value <= 0)
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_INCIDENT_ID",
                    "IncidentID must be greater than 0."));
            }

            return ExecuteFaultMonQuery(
                () => _faultMonService.GetFaultListDetail(selectedIncidentId.Value),
                "FAULTMON_DETAIL_QUERY_FAILED");
        }

        [HttpGet("faults/{incidentId:int}/popup")]
        [HttpGet("/Fault/GetFaultListDetailPop")]
        [ProducesResponseType(typeof(List<Dictionary<string, object?>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<Dictionary<string, object?>>> GetFaultListDetailPop([FromQuery(Name = "IncidentID")] int? legacyIncidentId, int? incidentId = null)
        {
            var selectedIncidentId = incidentId ?? legacyIncidentId;
            if (!selectedIncidentId.HasValue || selectedIncidentId.Value <= 0)
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_INCIDENT_ID",
                    "IncidentID must be greater than 0."));
            }

            return ExecuteFaultMonQuery(
                () => _faultMonService.GetFaultListDetailPop(selectedIncidentId.Value),
                "FAULTMON_DETAIL_POP_QUERY_FAILED");
        }

        private ActionResult<List<Dictionary<string, object?>>> ExecuteFaultMonQuery(
            Func<List<Dictionary<string, object?>>> query,
            string errorCode)
        {
            try
            {
                return Ok(query());
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        errorCode,
                        "An unexpected error occurred while retrieving FaultMon data."));
            }
        }
    }
}
