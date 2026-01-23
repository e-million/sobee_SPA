using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ApiControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Admin-only health check for the admin area.
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { status = "ok", area = "admin" });

        /// <summary>
        /// Admin-only summary of orders and revenue metrics.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _dashboardService.GetSummaryAsync();
            return FromServiceResult(result);
        }

        /// <summary>
        /// Admin-only daily order counts and revenue for the last N days.
        /// </summary>
        [HttpGet("orders-per-day")]
        public async Task<IActionResult> GetOrdersPerDay([FromQuery] int days = 30)
        {
            var result = await _dashboardService.GetOrdersPerDayAsync(days);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Admin-only list of products with low stock.
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 5)
        {
            var result = await _dashboardService.GetLowStockAsync(threshold);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Admin-only list of top-selling products.
        /// </summary>
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5)
        {
            var result = await _dashboardService.GetTopProductsAsync(limit);
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
