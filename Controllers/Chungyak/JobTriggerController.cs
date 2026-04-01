using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;

namespace SeinServices.Api.Controllers.Chungyak
{
    /// <summary>
    /// Timer Trigger 기반 배치 호출을 위한 깨우기/헬스 체크 API를 제공합니다.
    /// </summary>
    [ApiController]
    [Route("api/job-trigger")]
    public class JobTriggerController : SeinServices.Api.Controllers.BaseController
    {
        private readonly IConfiguration _configuration;

        public JobTriggerController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 앱 웜업을 위해 빠르게 응답하는 경량 엔드포인트입니다.
        /// </summary>
        [HttpGet("warmup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public ActionResult Warmup()
        {
            if (!TryAuthorizeJobRequest(_configuration, out var unauthorizedResult))
            {
                return unauthorizedResult!;
            }

            return Ok(new
            {
                message = "warmup-ok",
                utc = DateTime.UtcNow
            });
        }
    }
}

