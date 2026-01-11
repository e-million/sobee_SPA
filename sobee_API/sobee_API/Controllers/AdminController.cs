using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;

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

        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { status = "ok", area = "admin" });


        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalOrders = await _db.Torders.CountAsync();

            var totalRevenue = await _db.Torders
                .Where(o => o.DecTotalAmount != null)
                .SumAsync(o => o.DecTotalAmount) ?? 0m;

            var totalDiscounts = await _db.Torders
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


        [HttpGet("orders-per-day")]
        public async Task<IActionResult> GetOrdersPerDay([FromQuery] int days = 30)
        {
            if (days <= 0 || days > 365)
                return BadRequest(new { error = "Days must be between 1 and 365." });

            var fromDate = DateTime.UtcNow.Date.AddDays(-days);

            var data = await _db.Torders
                .Where(o => o.DtmOrderDate >= fromDate)
                .GroupBy(o => o.DtmOrderDate)
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

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 5)
        {
            if (threshold < 0)
                return BadRequest(new { error = "Threshold cannot be negative." });

            var products = await _db.Tproducts
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

        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5)
        {
            if (limit <= 0 || limit > 50)
                return BadRequest(new { error = "Limit must be between 1 and 50." });

            var products = await _db.TorderItems
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
