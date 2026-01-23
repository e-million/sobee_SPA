using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin/analytics")]
    [Authorize(Roles = "Admin")]
    public class AdminAnalyticsController : ApiControllerBase
    {
        private readonly IAdminAnalyticsService _analyticsService;

        public AdminAnalyticsController(IAdminAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueByPeriod(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string granularity = "day")
        {
            var result = await _analyticsService.GetRevenueByPeriodAsync(startDate, endDate, granularity);
            return FromServiceResult(result);
        }

        [HttpGet("orders/status")]
        public async Task<IActionResult> GetOrderStatusBreakdown()
        {
            var result = await _analyticsService.GetOrderStatusBreakdownAsync();
            return FromServiceResult(result);
        }

        [HttpGet("reviews/distribution")]
        public async Task<IActionResult> GetRatingDistribution()
        {
            var result = await _analyticsService.GetRatingDistributionAsync();
            return FromServiceResult(result);
        }

        [HttpGet("reviews/recent")]
        public async Task<IActionResult> GetRecentReviews([FromQuery] int limit = 5)
        {
            var result = await _analyticsService.GetRecentReviewsAsync(limit);
            return FromServiceResult(result);
        }

        [HttpGet("products/worst")]
        public async Task<IActionResult> GetWorstProducts([FromQuery] int limit = 5)
        {
            var result = await _analyticsService.GetWorstProductsAsync(limit);
            return FromServiceResult(result);
        }

        [HttpGet("products/categories")]
        public async Task<IActionResult> GetCategoryPerformance(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var result = await _analyticsService.GetCategoryPerformanceAsync(startDate, endDate);
            return FromServiceResult(result);
        }

        [HttpGet("inventory/summary")]
        public async Task<IActionResult> GetInventorySummary([FromQuery] int lowStockThreshold = 5)
        {
            var result = await _analyticsService.GetInventorySummaryAsync(lowStockThreshold);
            return FromServiceResult(result);
        }

        [HttpGet("orders/fulfillment")]
        public async Task<IActionResult> GetFulfillmentMetrics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var result = await _analyticsService.GetFulfillmentMetricsAsync(startDate, endDate);
            return FromServiceResult(result);
        }

        [HttpGet("customers/breakdown")]
        public async Task<IActionResult> GetCustomerBreakdown(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var result = await _analyticsService.GetCustomerBreakdownAsync(startDate, endDate);
            return FromServiceResult(result);
        }

        [HttpGet("customers/growth")]
        public async Task<IActionResult> GetCustomerGrowth(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string granularity = "day")
        {
            var result = await _analyticsService.GetCustomerGrowthAsync(startDate, endDate, granularity);
            return FromServiceResult(result);
        }

        [HttpGet("customers/top")]
        public async Task<IActionResult> GetTopCustomers(
            [FromQuery] int limit = 5,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _analyticsService.GetTopCustomersAsync(limit, startDate, endDate);
            return FromServiceResult(result);
        }

        [HttpGet("wishlist/top")]
        public async Task<IActionResult> GetMostWishlisted([FromQuery] int limit = 5)
        {
            var result = await _analyticsService.GetMostWishlistedAsync(limit);
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
