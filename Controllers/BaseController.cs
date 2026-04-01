using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;

namespace SeinServices.Api.Controllers
{
    /// <summary>
    /// 공통 API 응답 기능을 제공하는 기본 컨트롤러입니다.
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// 에러 응답 객체를 생성합니다.
        /// </summary>
        /// <param name="code">에러 코드</param>
        /// <param name="message">에러 메시지</param>
        /// <returns>에러 응답 객체</returns>
        protected ErrorResponseDto CreateErrorResponse(string code, string message)
        {
            return new ErrorResponseDto
            {
                Code = code,
                Message = message,
                TraceId = HttpContext.TraceIdentifier
            };
        }

        /// <summary>
        /// Timer Trigger endpoint 요청의 API key를 검증합니다.
        /// </summary>
        protected bool TryAuthorizeJobRequest(IConfiguration configuration, out ActionResult? unauthorizedResult)
        {
            var configuredApiKey = configuration["JobTrigger:ApiKey"];
            if (string.IsNullOrWhiteSpace(configuredApiKey))
            {
                unauthorizedResult = StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CreateErrorResponse(
                        "JOB_TRIGGER_CONFIG_MISSING",
                        "Job trigger API key is not configured."));
                return false;
            }

            if (!Request.Headers.TryGetValue("X-Job-Key", out var requestApiKey)
                || !string.Equals(requestApiKey.ToString(), configuredApiKey, StringComparison.Ordinal))
            {
                unauthorizedResult = Unauthorized(CreateErrorResponse(
                    "JOB_TRIGGER_UNAUTHORIZED",
                    "Invalid job trigger API key."));
                return false;
            }

            unauthorizedResult = null;
            return true;
        }
    }
}

