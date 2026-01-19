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
        private readonly ApplicationDbContext _identityDb;

        public AdminAnalyticsController(SobeecoredbContext db, ApplicationDbContext identityDb)
        {
            _db = db;
            _identityDb = identityDb;
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

        [HttpGet("products/categories")]
        public async Task<IActionResult> GetCategoryPerformance(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
            {
                return BadRequest(new { error });
            }

            var productCounts = await _db.Tproducts
                .Include(p => p.IntDrinkCategory)
                .GroupBy(p => new
                {
                    p.IntDrinkCategoryId,
                    categoryName = p.IntDrinkCategory != null ? p.IntDrinkCategory.StrName : null
                })
                .Select(g => new
                {
                    categoryId = g.Key.IntDrinkCategoryId,
                    categoryName = g.Key.categoryName,
                    productCount = g.Count()
                })
                .ToListAsync();

            var sales = await _db.TorderItems
                .Where(i => i.IntOrder != null
                    && i.IntOrder.DtmOrderDate != null
                    && i.IntOrder.DtmOrderDate >= start
                    && i.IntOrder.DtmOrderDate <= end)
                .Select(i => new
                {
                    categoryId = i.IntProduct.IntDrinkCategoryId,
                    categoryName = i.IntProduct.IntDrinkCategory != null ? i.IntProduct.IntDrinkCategory.StrName : null,
                    unitsSold = i.IntQuantity ?? 0,
                    revenue = (i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m)
                })
                .ToListAsync();

            var salesLookup = sales
                .GroupBy(item => new { item.categoryId, item.categoryName })
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        unitsSold = g.Sum(x => x.unitsSold),
                        revenue = g.Sum(x => x.revenue)
                    });

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<CategoryPerformanceRow>();

            foreach (var item in productCounts)
            {
                var key = new
                {
                    categoryId = item.categoryId,
                    categoryName = item.categoryName
                };
                var displayName = string.IsNullOrWhiteSpace(item.categoryName)
                    ? "Uncategorized"
                    : item.categoryName!;

                salesLookup.TryGetValue(key, out var salesData);

                results.Add(new CategoryPerformanceRow
                {
                    CategoryId = item.categoryId,
                    CategoryName = displayName,
                    ProductCount = item.productCount,
                    UnitsSold = salesData?.unitsSold ?? 0,
                    Revenue = salesData?.revenue ?? 0m
                });

                seenKeys.Add($"{item.categoryId ?? 0}:{displayName}");
            }

            foreach (var entry in salesLookup)
            {
                var displayName = string.IsNullOrWhiteSpace(entry.Key.categoryName)
                    ? "Uncategorized"
                    : entry.Key.categoryName!;
                var identity = $"{entry.Key.categoryId ?? 0}:{displayName}";
                if (seenKeys.Contains(identity))
                {
                    continue;
                }

                results.Add(new CategoryPerformanceRow
                {
                    CategoryId = entry.Key.categoryId,
                    CategoryName = displayName,
                    ProductCount = 0,
                    UnitsSold = entry.Value.unitsSold,
                    Revenue = entry.Value.revenue
                });
            }

            var ordered = results
                .OrderByDescending(item => item.Revenue)
                .ThenBy(item => item.CategoryName)
                .ToList();

            return Ok(ordered);
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
            var totalStockValue = await _db.Tproducts
                .SumAsync(p => (p.DecCost ?? 0m) * p.IntStockAmount);

            return Ok(new
            {
                totalProducts,
                inStockCount,
                lowStockCount,
                outOfStockCount,
                totalStockValue
            });
        }

        [HttpGet("orders/fulfillment")]
        public async Task<IActionResult> GetFulfillmentMetrics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
            {
                return BadRequest(new { error });
            }

            var currentData = await _db.Torders
                .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
                .Select(o => new
                {
                    orderDate = o.DtmOrderDate!.Value,
                    shippedDate = o.DtmShippedDate,
                    deliveredDate = o.DtmDeliveredDate
                })
                .ToListAsync();

            var avgHoursToShip = CalculateAverageHours(currentData.Select(item => (item.orderDate, item.shippedDate)));
            var avgHoursToDeliver = CalculateAverageHours(currentData.Select(item => (item.orderDate, item.deliveredDate)));

            var periodDays = Math.Max(1, (end - start).TotalDays);
            var previousStart = start.AddDays(-periodDays);
            var previousEnd = start;

            var previousData = await _db.Torders
                .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= previousStart && o.DtmOrderDate < previousEnd)
                .Select(o => new
                {
                    orderDate = o.DtmOrderDate!.Value,
                    shippedDate = o.DtmShippedDate
                })
                .ToListAsync();

            var previousAvgShip = CalculateAverageHours(previousData.Select(item => (item.orderDate, item.shippedDate)));
            var trend = previousAvgShip <= 0 ? 0 : Math.Round(((avgHoursToShip - previousAvgShip) / previousAvgShip) * 100, 2);

            return Ok(new
            {
                avgHoursToShip = Math.Round(avgHoursToShip, 2),
                avgHoursToDeliver = Math.Round(avgHoursToDeliver, 2),
                trend
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

        [HttpGet("customers/growth")]
        public async Task<IActionResult> GetCustomerGrowth(
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

            var baselineCount = await _identityDb.Users.CountAsync(u => u.CreatedDate < start);

            var registrations = await _identityDb.Users
                .Where(u => u.CreatedDate >= start && u.CreatedDate <= end)
                .Select(u => u.CreatedDate)
                .ToListAsync();

            var grouped = registrations
                .GroupBy(date => GetPeriodStart(date, normalizedGranularity))
                .Select(g => new
                {
                    date = g.Key,
                    newRegistrations = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            var runningTotal = baselineCount;
            var result = new List<object>(grouped.Count);

            foreach (var entry in grouped)
            {
                runningTotal += entry.newRegistrations;
                result.Add(new
                {
                    date = entry.date,
                    newRegistrations = entry.newRegistrations,
                    cumulativeTotal = runningTotal
                });
            }

            return Ok(result);
        }

        [HttpGet("customers/top")]
        public async Task<IActionResult> GetTopCustomers(
            [FromQuery] int limit = 5,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (limit <= 0 || limit > 50)
            {
                return BadRequest(new { error = "Limit must be between 1 and 50." });
            }

            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MaxValue;
            if (startDate != null || endDate != null)
            {
                if (!TryNormalizeDateRange(startDate, endDate, out start, out end, out var error))
                {
                    return BadRequest(new { error });
                }
            }

            var baseQuery = _db.Torders
                .Where(o => !string.IsNullOrEmpty(o.UserId));

            if (start != DateTime.MinValue || end != DateTime.MaxValue)
            {
                baseQuery = baseQuery.Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end);
            }

            var topCustomers = await baseQuery
                .GroupBy(o => o.UserId!)
                .Select(g => new
                {
                    userId = g.Key,
                    totalSpent = g.Sum(o => o.DecTotalAmount ?? 0m),
                    orderCount = g.Count(),
                    lastOrderDate = g.Max(o => o.DtmOrderDate)
                })
                .OrderByDescending(x => x.totalSpent)
                .Take(limit)
                .ToListAsync();

            var userIds = topCustomers.Select(x => x.userId).ToList();
            var users = await _identityDb.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.strFirstName,
                    u.strLastName
                })
                .ToListAsync();

            var userLookup = users.ToDictionary(u => u.Id, u => u);

            var response = topCustomers.Select(customer =>
            {
                userLookup.TryGetValue(customer.userId, out var user);
                var name = user == null
                    ? null
                    : string.Join(" ", new[] { user.strFirstName, user.strLastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

                return new
                {
                    userId = customer.userId,
                    email = user?.Email,
                    name = string.IsNullOrWhiteSpace(name) ? null : name,
                    totalSpent = customer.totalSpent,
                    orderCount = customer.orderCount,
                    lastOrderDate = customer.lastOrderDate
                };
            });

            return Ok(response);
        }

        [HttpGet("wishlist/top")]
        public async Task<IActionResult> GetMostWishlisted([FromQuery] int limit = 5)
        {
            if (limit <= 0 || limit > 50)
            {
                return BadRequest(new { error = "Limit must be between 1 and 50." });
            }

            var items = await _db.Tfavorites
                .GroupBy(f => new { f.IntProductId, f.IntProduct.StrName })
                .Select(g => new
                {
                    productId = g.Key.IntProductId,
                    name = g.Key.StrName,
                    wishlistCount = g.Count()
                })
                .OrderByDescending(x => x.wishlistCount)
                .Take(limit)
                .ToListAsync();

            return Ok(items);
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

        private static double CalculateAverageHours(IEnumerable<(DateTime OrderDate, DateTime? EndDate)> rows)
        {
            var durations = rows
                .Where(row => row.EndDate != null)
                .Select(row => (row.EndDate!.Value - row.OrderDate).TotalHours)
                .ToList();

            return durations.Count == 0 ? 0 : durations.Average();
        }

        private sealed class CategoryPerformanceRow
        {
            public int? CategoryId { get; init; }
            public string CategoryName { get; init; } = "Uncategorized";
            public int ProductCount { get; init; }
            public int UnitsSold { get; init; }
            public decimal Revenue { get; init; }
        }
    }
}
