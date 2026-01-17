using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sobee_API.Constants;
using Sobee.Domain.Data;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin/analytics")]
    [Authorize(Roles = "Admin")]
    public class AdminAnalyticsController : ControllerBase
    {
        private readonly SobeecoredbContext _db;

        public AdminAnalyticsController(SobeecoredbContext db)
        {
            _db = db;
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueByPeriod(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string granularity = "day")
        {
            if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
            {
                return BadRequest(new { error });
            }

            if (!TryNormalizeGranularity(granularity, out var normalizedGranularity))
            {
                return BadRequest(new { error = "Granularity must be day, week, or month." });
            }

            var orders = await _db.Torders
                .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
                .Select(o => new
                {
                    date = o.DtmOrderDate!.Value,
                    revenue = o.DecTotalAmount ?? 0m
                })
                .ToListAsync();

            var grouped = orders
                .GroupBy(o => GetPeriodStart(o.date, normalizedGranularity))
                .Select(g => new
                {
                    date = g.Key,
                    revenue = g.Sum(x => x.revenue),
                    orderCount = g.Count(),
                    avgOrderValue = g.Count() == 0 ? 0m : g.Sum(x => x.revenue) / g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Ok(grouped);
        }

        [HttpGet("orders/status")]
        public async Task<IActionResult> GetOrderStatusBreakdown()
        {
            var rawStatuses = await _db.Torders
                .Select(o => o.StrOrderStatus)
                .ToListAsync();

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var status in rawStatuses)
            {
                var normalized = string.IsNullOrWhiteSpace(status) ? OrderStatuses.Pending : status.Trim();
                if (!OrderStatuses.IsKnown(normalized))
                {
                    normalized = "Other";
                }
                else
                {
                    normalized = OrderStatuses.Normalize(normalized);
                }

                counts[normalized] = counts.TryGetValue(normalized, out var current) ? current + 1 : 1;
            }

            int Count(string key) => counts.TryGetValue(key, out var value) ? value : 0;

            return Ok(new
            {
                total = rawStatuses.Count,
                pending = Count(OrderStatuses.Pending),
                paid = Count(OrderStatuses.Paid),
                processing = Count(OrderStatuses.Processing),
                shipped = Count(OrderStatuses.Shipped),
                delivered = Count(OrderStatuses.Delivered),
                cancelled = Count(OrderStatuses.Cancelled),
                refunded = Count(OrderStatuses.Refunded),
                other = Count("Other")
            });
        }

        [HttpGet("reviews/distribution")]
        public async Task<IActionResult> GetRatingDistribution()
        {
            var ratings = await _db.Treviews
                .Select(r => r.IntRating)
                .ToListAsync();

            var total = ratings.Count;
            var average = total == 0 ? 0m : (decimal)ratings.Average();

            var distribution = new int[5];
            foreach (var rating in ratings)
            {
                if (rating >= 1 && rating <= 5)
                {
                    distribution[rating - 1]++;
                }
            }

            return Ok(new
            {
                averageRating = average,
                totalReviews = total,
                distribution = new
                {
                    oneStar = distribution[0],
                    twoStar = distribution[1],
                    threeStar = distribution[2],
                    fourStar = distribution[3],
                    fiveStar = distribution[4]
                }
            });
        }

        [HttpGet("reviews/recent")]
        public async Task<IActionResult> GetRecentReviews([FromQuery] int limit = 5)
        {
            if (limit <= 0 || limit > 50)
            {
                return BadRequest(new { error = "Limit must be between 1 and 50." });
            }

            var reviews = await _db.Treviews
                .OrderByDescending(r => r.DtmReviewDate)
                .Take(limit)
                .Select(r => new
                {
                    reviewId = r.IntReviewId,
                    productId = r.IntProductId,
                    productName = r.IntProduct.StrName,
                    rating = r.IntRating,
                    comment = r.StrReviewText,
                    createdAt = r.DtmReviewDate,
                    userId = r.UserId,
                    hasReplies = r.TReviewReplies.Any()
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("products/worst")]
        public async Task<IActionResult> GetWorstProducts([FromQuery] int limit = 5)
        {
            if (limit <= 0 || limit > 50)
            {
                return BadRequest(new { error = "Limit must be between 1 and 50." });
            }

            var products = await _db.Tproducts
                .Select(p => new
                {
                    productId = p.IntProductId,
                    name = p.StrName,
                    unitsSold = p.TorderItems.Sum(i => (int?)i.IntQuantity) ?? 0,
                    revenue = p.TorderItems.Sum(i => (decimal?)((i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m))) ?? 0m
                })
                .OrderBy(p => p.unitsSold)
                .ThenBy(p => p.name)
                .Take(limit)
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("inventory/summary")]
        public async Task<IActionResult> GetInventorySummary([FromQuery] int lowStockThreshold = 5)
        {
            if (lowStockThreshold < 0)
            {
                return BadRequest(new { error = "Threshold cannot be negative." });
            }

            var totalProducts = await _db.Tproducts.CountAsync();
            var outOfStockCount = await _db.Tproducts.CountAsync(p => p.IntStockAmount <= 0);
            var inStockCount = await _db.Tproducts.CountAsync(p => p.IntStockAmount > 0);
            var lowStockCount = await _db.Tproducts.CountAsync(p => p.IntStockAmount > 0 && p.IntStockAmount <= lowStockThreshold);

            return Ok(new
            {
                totalProducts,
                inStockCount,
                lowStockCount,
                outOfStockCount
            });
        }

        [HttpGet("customers/breakdown")]
        public async Task<IActionResult> GetCustomerBreakdown(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
            {
                return BadRequest(new { error });
            }

            var orders = await _db.Torders
                .Where(o => o.UserId != null && o.DtmOrderDate != null)
                .Select(o => new
                {
                    userId = o.UserId!,
                    date = o.DtmOrderDate!.Value,
                    total = o.DecTotalAmount ?? 0m
                })
                .ToListAsync();

            var firstOrderByUser = orders
                .GroupBy(o => o.userId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Min(x => x.date), StringComparer.OrdinalIgnoreCase);

            var ordersInRange = orders
                .Where(o => o.date >= start && o.date <= end)
                .ToList();

            var newCustomerIds = firstOrderByUser
                .Where(kvp => kvp.Value >= start && kvp.Value <= end)
                .Select(kvp => kvp.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var returningCustomerIds = ordersInRange
                .Select(o => o.userId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(userId => firstOrderByUser.TryGetValue(userId, out var firstOrderDate) && firstOrderDate < start)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newCustomerRevenue = ordersInRange
                .Where(o => newCustomerIds.Contains(o.userId))
                .Sum(o => o.total);

            var returningCustomerRevenue = ordersInRange
                .Where(o => returningCustomerIds.Contains(o.userId))
                .Sum(o => o.total);

            return Ok(new
            {
                newCustomers = newCustomerIds.Count,
                returningCustomers = returningCustomerIds.Count,
                newCustomerRevenue,
                returningCustomerRevenue
            });
        }

        private static bool TryNormalizeDateRange(
            DateTime? startDate,
            DateTime? endDate,
            out DateTime start,
            out DateTime end,
            out string? error)
        {
            end = endDate ?? DateTime.UtcNow;
            start = startDate ?? end.AddDays(-30);

            if (start > end)
            {
                error = "startDate must be before endDate.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryNormalizeGranularity(string? granularity, out string normalized)
        {
            normalized = (granularity ?? "day").Trim().ToLowerInvariant();
            return normalized is "day" or "week" or "month";
        }

        private static DateTime GetPeriodStart(DateTime date, string granularity)
        {
            return granularity switch
            {
                "week" => GetWeekStart(date),
                "month" => new DateTime(date.Year, date.Month, 1),
                _ => date.Date
            };
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + ((int)date.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }
    }
}
