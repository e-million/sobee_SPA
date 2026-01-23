using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ApiControllerBase
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        /// <summary>
        /// Health check for API and database connectivity.
        /// </summary>
        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var result = await _homeService.GetPingAsync();
            return FromServiceResult(result);
        }

        private IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Value);
            }

            var code = result.ErrorCode ?? "ServerError";
            var message = result.ErrorMessage ?? "An unexpected error occurred.";

            return code switch
            {
                "NotFound" => NotFoundError(message, code, result.ErrorData),
                "ValidationError" => BadRequestError(message, code, result.ErrorData),
                "Unauthorized" => UnauthorizedError(message, code, result.ErrorData),
                "Forbidden" => ForbiddenError(message, code, result.ErrorData),
                "Conflict" => ConflictError(message, code, result.ErrorData),
                _ => ServerError(message, code, result.ErrorData)
            };
        }
    }
}
