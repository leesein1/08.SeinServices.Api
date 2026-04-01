using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;

namespace SeinServices.Api.Controllers.Chungyak
{
    [ApiController]
    [Route("api/job-trigger")]
    /// <summary>
    /// JobTriggerController 관련 기능을 제공합니다.
    /// </summary>
    public class JobTriggerController : SeinServices.Api.Controllers.BaseController
    {
        private readonly IConfiguration _configuration;

        public JobTriggerController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("warmup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Warmup 작업을 수행합니다.
        /// </summary>
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

