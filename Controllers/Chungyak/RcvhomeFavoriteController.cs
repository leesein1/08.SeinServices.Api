using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;
using SeinServices.Api.Models.Chungyak.Requests;
using SeinServices.Api.Models.Chungyak.Responses;
using SeinServices.Api.Services.Chungyak;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/rcvhome-favorites")]
    /// <summary>
    /// RcvhomeFavoriteController 관련 기능을 제공합니다.
    /// </summary>
    public class RcvhomeFavoriteController : SeinServices.Api.Controllers.BaseController
    {
        private readonly ChungyakSearchService _chungyakSearchService;
        private readonly ChungyakFavoriteService _chungyakFavoriteService;

        public RcvhomeFavoriteController(
            ChungyakSearchService chungyakSearchService,
            ChungyakFavoriteService chungyakFavoriteService)
        {
            _chungyakSearchService = chungyakSearchService;
            _chungyakFavoriteService = chungyakFavoriteService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<RcvhomeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// GetFavoriteRcvhomes 작업을 수행합니다.
        /// </summary>
        public ActionResult<List<RcvhomeResponseDto>> GetFavoriteRcvhomes([FromQuery] RcvhomesRequestDto request)
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
                var response = _chungyakSearchService.GetFavoriteRcvhomes(request);
                return Ok(response);
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "FAVORITE_RCVHOME_QUERY_FAILED",
                        "An unexpected error occurred while retrieving favorite rcvhome data."));
            }
        }

        [HttpPost("{pblancId}")]
        [ProducesResponseType(typeof(FavoriteMutationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// AddFavorite 작업을 수행합니다.
        /// </summary>
        public ActionResult<FavoriteMutationResponseDto> AddFavorite(string pblancId)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_PBLANC_ID",
                    "PblancId is required."));
            }

            try
            {
                var response = _chungyakFavoriteService.AddFavorite(pblancId);
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
                        "FAVORITE_ADD_FAILED",
                        "An unexpected error occurred while adding favorite data."));
            }
        }

        [HttpDelete("{pblancId}")]
        [ProducesResponseType(typeof(FavoriteMutationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// RemoveFavorite 작업을 수행합니다.
        /// </summary>
        public ActionResult<FavoriteMutationResponseDto> RemoveFavorite(string pblancId)
        {
            if (string.IsNullOrWhiteSpace(pblancId))
            {
                return BadRequest(CreateErrorResponse(
                    "INVALID_PBLANC_ID",
                    "PblancId is required."));
            }

            try
            {
                var response = _chungyakFavoriteService.RemoveFavorite(pblancId);
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
                        "FAVORITE_REMOVE_FAILED",
                        "An unexpected error occurred while removing favorite data."));
            }
        }
    }
}


