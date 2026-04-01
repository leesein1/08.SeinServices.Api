using Microsoft.AspNetCore.Mvc;
using SeinServices.Api.Models.Common;

namespace SeinServices.Api.Controllers
{
    /// <summary>
    /// BaseController 관련 기능을 제공합니다.
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
        protected ErrorResponseDto CreateErrorResponse(string code, string message)
        {
            return new ErrorResponseDto
            {
                Code = code,
                Message = message,
                TraceId = HttpContext.TraceIdentifier
            };
        }

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

