using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    /// <summary>
    /// 청약 모집공고 조회 API를 제공하는 컨트롤러입니다.
    /// </summary>
    [ApiController]
    [Route("api/rcvhome-search")]
    public class RcvhomeSearchController : SeinServices.Api.Controllers.BaseController
    {
        private readonly ChungyakSearchService _chungyakSearchService;

        public RcvhomeSearchController(ChungyakSearchService chungyakSearchService)
        {
            _chungyakSearchService = chungyakSearchService;
        }

        /// <summary>
        /// 모집공고 목록을 조회합니다.
        /// </summary>
        [HttpGet("rcvhomes")]
        [ProducesResponseType(typeof(List<RcvhomeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<RcvhomeResponseDto>> GetRcvhomes([FromQuery] RcvhomesRequestDto request)
        {
            return ExecuteSearch(request, _chungyakSearchService.GetRcvhomes, "RCVHOME_QUERY_FAILED");
        }

        /// <summary>
        /// 오늘 기준 마감임박(D-7) 모집공고 목록을 조회합니다.
        /// </summary>
        [HttpGet("deadline-soon")]
        [ProducesResponseType(typeof(List<RcvhomeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult<List<RcvhomeResponseDto>> GetDeadlineSoonRcvhomes([FromQuery] RcvhomesRequestDto request)
        {
            return ExecuteSearch(request, _chungyakSearchService.GetDeadlineSoonRcvhomes, "DEADLINE_SOON_RCVHOME_QUERY_FAILED");
        }

        /// <summary>
        /// 모집공고 고유번호를 기준으로 상세 정보를 조회합니다.
        /// </summary>
        /// <param name="pblancId">모집공고 고유번호</param>
        [HttpGet("rcvhomes/{pblancId}")]
        [ProducesResponseType(typeof(RcvhomeDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// 요청 검증과 공통 예외 처리를 수행하는 헬퍼 메서드입니다.
        /// 실제 검색 로직은 searchFunc 매개변수로 전달된 함수를 통해 수행됩니다.
        /// errorCode 매개변수는 예외 발생 시 반환할 에러 응답의 코드로 사용됩니다.
        /// </summary> 
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

