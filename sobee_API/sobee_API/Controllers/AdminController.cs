using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using sobee_API.DTOs.Common;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly SobeecoredbContext _db;

        public AdminController(SobeecoredbContext db)
        {
            _db = db;
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
            var totalOrders = await _db.Torders.AsNoTracking().CountAsync();

            var totalRevenue = await _db.Torders
                .AsNoTracking()
                .Where(o => o.DecTotalAmount != null)
                .SumAsync(o => o.DecTotalAmount) ?? 0m;

            var totalDiscounts = await _db.Torders
                .AsNoTracking()
                .Where(o => o.DecDiscountAmount != null)
                .SumAsync(o => o.DecDiscountAmount) ?? 0m;

            var averageOrderValue = totalOrders == 0
                ? 0m
                : totalRevenue / totalOrders;

            return Ok(new
            {
                totalOrders,
                totalRevenue,
                totalDiscounts,
                averageOrderValue
            });
        }


        /// <summary>
        /// Admin-only daily order counts and revenue for the last N days.
        /// </summary>
        [HttpGet("orders-per-day")]
        public async Task<IActionResult> GetOrdersPerDay([FromQuery] int days = 30)
        {
            if (days <= 0 || days > 365)
                return BadRequest(new ApiErrorResponse("Days must be between 1 and 365.", "ValidationError"));

            var fromDate = DateTime.UtcNow.Date.AddDays(-days);

            var data = await _db.Torders
                .AsNoTracking()
                .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= fromDate)
                .GroupBy(o => o.DtmOrderDate!.Value.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count(),
                    revenue = g.Sum(o => o.DecTotalAmount) ?? 0m
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Admin-only list of products with low stock.
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 5)
        {
            if (threshold < 0)
                return BadRequest(new ApiErrorResponse("Threshold cannot be negative.", "ValidationError"));

            var products = await _db.Tproducts
                .AsNoTracking()
                .Where(p => p.IntStockAmount <= threshold)
                .OrderBy(p => p.IntStockAmount)
                .Select(p => new
                {
                    productId = p.IntProductId,
                    name = p.StrName,
                    stockAmount = p.IntStockAmount
                })
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// Admin-only list of top-selling products.
        /// </summary>
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5)
        {
            if (limit <= 0 || limit > 50)
                return BadRequest(new ApiErrorResponse("Limit must be between 1 and 50.", "ValidationError"));

            var products = await _db.TorderItems
                .AsNoTracking()
                .GroupBy(i => new
                {
                    i.IntProductId,
                    i.IntProduct.StrName
                })
                .Select(g => new
                {
                    productId = g.Key.IntProductId,
                    name = g.Key.StrName,
                    quantitySold = g.Sum(x => x.IntQuantity ?? 0),
                    revenue = g.Sum(x => (x.IntQuantity ?? 0) * (x.MonPricePerUnit ?? 0m))
                })
                .OrderByDescending(x => x.quantitySold)
                .Take(limit)
                .ToListAsync();

            return Ok(products);
        }











    }
}
