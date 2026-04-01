using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/rcvhome-search")]
    /// <summary>
    /// RcvhomeSearchController 관련 기능을 제공합니다.
    /// </summary>
    public class RcvhomeSearchController : SeinServices.Api.Controllers.BaseController
    {
        private readonly ChungyakSearchService _chungyakSearchService;

        public RcvhomeSearchController(ChungyakSearchService chungyakSearchService)
        {
            _chungyakSearchService = chungyakSearchService;
        }

        [HttpGet("rcvhomes")]
        [ProducesResponseType(typeof(List<RcvhomeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// GetRcvhomes 작업을 수행합니다.
        /// </summary>
        public ActionResult<List<RcvhomeResponseDto>> GetRcvhomes([FromQuery] RcvhomesRequestDto request)
        {
            return ExecuteSearch(request, _chungyakSearchService.GetRcvhomes, "RCVHOME_QUERY_FAILED");
        }

        [HttpGet("deadline-soon")]
        [ProducesResponseType(typeof(List<RcvhomeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// GetDeadlineSoonRcvhomes 작업을 수행합니다.
        /// </summary>
        public ActionResult<List<RcvhomeResponseDto>> GetDeadlineSoonRcvhomes([FromQuery] RcvhomesRequestDto request)
        {
            return ExecuteSearch(request, _chungyakSearchService.GetDeadlineSoonRcvhomes, "DEADLINE_SOON_RCVHOME_QUERY_FAILED");
        }

        [HttpGet("rcvhomes/{pblancId}")]
        [ProducesResponseType(typeof(RcvhomeDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// GetRcvhomeDetail 작업을 수행합니다.
        /// </summary>
        public ActionResult<RcvhomeDetailResponseDto> GetRcvhomeDetail(string pblancId)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_PBLANC_ID",
                    "PblancId is required."));
            }

            try
            {
                var response = _chungyakSearchService.GetRcvhomeDetail(pblancId);

                if (response is null)
                {
                    return NotFound(CreateErrorResponse(
                        "RCVHOME_NOT_FOUND",
                        "The requested rcvhome data was not found."));
                }

                return Ok(response);
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "RCVHOME_DETAIL_QUERY_FAILED",
                        "An unexpected error occurred while retrieving rcvhome detail data."));
            }
        }

        private ActionResult<List<RcvhomeResponseDto>> ExecuteSearch(
            RcvhomesRequestDto request,
            Func<RcvhomesRequestDto, List<RcvhomeResponseDto>> searchFunc,
            string errorCode)
        {
            if (!_chungyakSearchService.IsValidStatus(request.Status))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_STATUS",
                    "Status must be one of: 전체, 접수예정, 접수중, 접수마감."));
            }

            if (!_chungyakSearchService.IsValidDateRange(request.BeginFrom, request.BeginTo))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_DATE_RANGE",
                    "BeginFrom must be less than or equal to BeginTo."));
            }

            try
            {
                var response = searchFunc(request);
                return Ok(response);
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        errorCode,
                        "An unexpected error occurred while retrieving rcvhome data."));
            }
        }
    }
}

