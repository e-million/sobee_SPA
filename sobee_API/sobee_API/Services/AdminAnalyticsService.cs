using sobee_API.Constants;
using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly IAdminAnalyticsRepository _analyticsRepository;
    private readonly IAdminUserRepository _userRepository;

    public AdminAnalyticsService(IAdminAnalyticsRepository analyticsRepository, IAdminUserRepository userRepository)
    {
        _analyticsRepository = analyticsRepository;
        _userRepository = userRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<RevenueByPeriodResponse>>> GetRevenueByPeriodAsync(
        DateTime? startDate,
        DateTime? endDate,
        string granularity)
    {
        if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
        {
            return Validation<IReadOnlyList<RevenueByPeriodResponse>>(error ?? "Invalid date range.", null);
        }

        if (!TryNormalizeGranularity(granularity, out var normalizedGranularity))
        {
            return Validation<IReadOnlyList<RevenueByPeriodResponse>>("Granularity must be day, week, or month.", null);
        }

        if (normalizedGranularity == "day")
        {
            var rows = await _analyticsRepository.GetRevenueByDayAsync(start, end);
            var response = rows.Select(row => new RevenueByPeriodResponse
            {
                Date = row.Date,
                Revenue = row.Revenue,
                OrderCount = row.OrderCount,
                AvgOrderValue = row.AvgOrderValue
            }).ToList();

            return ServiceResult<IReadOnlyList<RevenueByPeriodResponse>>.Ok(response);
        }

        var orders = await _analyticsRepository.GetRevenueRawAsync(start, end);
        var grouped = orders
            .GroupBy(o => GetPeriodStart(o.Date, normalizedGranularity))
            .Select(g => new RevenueByPeriodResponse
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Revenue),
                OrderCount = g.Count(),
                AvgOrderValue = g.Count() == 0 ? 0m : g.Sum(x => x.Revenue) / g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        return ServiceResult<IReadOnlyList<RevenueByPeriodResponse>>.Ok(grouped);
    }

    public async Task<ServiceResult<OrderStatusBreakdownResponse>> GetOrderStatusBreakdownAsync()
    {
        var rawStatuses = await _analyticsRepository.GetOrderStatusesAsync();
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

        return ServiceResult<OrderStatusBreakdownResponse>.Ok(new OrderStatusBreakdownResponse
        {
            Total = rawStatuses.Count,
            Pending = Count(OrderStatuses.Pending),
            Paid = Count(OrderStatuses.Paid),
            Processing = Count(OrderStatuses.Processing),
            Shipped = Count(OrderStatuses.Shipped),
            Delivered = Count(OrderStatuses.Delivered),
            Cancelled = Count(OrderStatuses.Cancelled),
            Refunded = Count(OrderStatuses.Refunded),
            Other = Count("Other")
        });
    }

    public async Task<ServiceResult<RatingDistributionResponse>> GetRatingDistributionAsync()
    {
        var ratings = await _analyticsRepository.GetReviewRatingsAsync();
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

        return ServiceResult<RatingDistributionResponse>.Ok(new RatingDistributionResponse
        {
            AverageRating = average,
            TotalReviews = total,
            Distribution = new RatingDistributionBreakdown
            {
                OneStar = distribution[0],
                TwoStar = distribution[1],
                ThreeStar = distribution[2],
                FourStar = distribution[3],
                FiveStar = distribution[4]
            }
        });
    }

    public async Task<ServiceResult<IReadOnlyList<RecentReviewResponse>>> GetRecentReviewsAsync(int limit)
    {
        if (limit <= 0 || limit > 50)
        {
            return Validation<IReadOnlyList<RecentReviewResponse>>("Limit must be between 1 and 50.", null);
        }

        var reviews = await _analyticsRepository.GetRecentReviewsAsync(limit);
        var response = reviews.Select(review => new RecentReviewResponse
        {
            ReviewId = review.ReviewId,
            ProductId = review.ProductId,
            ProductName = review.ProductName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            UserId = review.UserId,
            HasReplies = review.HasReplies
        }).ToList();

        return ServiceResult<IReadOnlyList<RecentReviewResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyList<WorstProductResponse>>> GetWorstProductsAsync(int limit)
    {
        if (limit <= 0 || limit > 50)
        {
            return Validation<IReadOnlyList<WorstProductResponse>>("Limit must be between 1 and 50.", null);
        }

        var products = await _analyticsRepository.GetWorstProductsAsync(limit);
        var response = products.Select(product => new WorstProductResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            UnitsSold = product.UnitsSold,
            Revenue = product.Revenue
        }).ToList();

        return ServiceResult<IReadOnlyList<WorstProductResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryPerformanceResponse>>> GetCategoryPerformanceAsync(
        DateTime? startDate,
        DateTime? endDate)
    {
        if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
        {
            return Validation<IReadOnlyList<CategoryPerformanceResponse>>(error ?? "Invalid date range.", null);
        }

        var productCounts = await _analyticsRepository.GetCategoryProductCountsAsync();
        var sales = await _analyticsRepository.GetCategorySalesAsync(start, end);

        var salesLookup = sales
            .GroupBy(item => new { item.CategoryId, item.CategoryName })
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    unitsSold = g.Sum(x => x.UnitsSold),
                    revenue = g.Sum(x => x.Revenue)
                });

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<CategoryPerformanceResponse>();

        foreach (var item in productCounts)
        {
            var key = new
            {
                item.CategoryId,
                item.CategoryName
            };
            var displayName = string.IsNullOrWhiteSpace(item.CategoryName)
                ? "Uncategorized"
                : item.CategoryName!;

            salesLookup.TryGetValue(key, out var salesData);

            results.Add(new CategoryPerformanceResponse
            {
                CategoryId = item.CategoryId,
                CategoryName = displayName,
                ProductCount = item.ProductCount,
                UnitsSold = salesData?.unitsSold ?? 0,
                Revenue = salesData?.revenue ?? 0m
            });

            seenKeys.Add($"{item.CategoryId ?? 0}:{displayName}");
        }

        foreach (var entry in salesLookup)
        {
            var displayName = string.IsNullOrWhiteSpace(entry.Key.CategoryName)
                ? "Uncategorized"
                : entry.Key.CategoryName!;
            var identity = $"{entry.Key.CategoryId ?? 0}:{displayName}";
            if (seenKeys.Contains(identity))
            {
                continue;
            }

            results.Add(new CategoryPerformanceResponse
            {
                CategoryId = entry.Key.CategoryId,
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

        return ServiceResult<IReadOnlyList<CategoryPerformanceResponse>>.Ok(ordered);
    }

    public async Task<ServiceResult<InventorySummaryResponse>> GetInventorySummaryAsync(int lowStockThreshold)
    {
        if (lowStockThreshold < 0)
        {
            return Validation<InventorySummaryResponse>("Threshold cannot be negative.", null);
        }

        var summary = await _analyticsRepository.GetInventorySummaryAsync(lowStockThreshold);
        return ServiceResult<InventorySummaryResponse>.Ok(new InventorySummaryResponse
        {
            TotalProducts = summary.TotalProducts,
            InStockCount = summary.InStockCount,
            LowStockCount = summary.LowStockCount,
            OutOfStockCount = summary.OutOfStockCount,
            TotalStockValue = summary.TotalStockValue
        });
    }

    public async Task<ServiceResult<FulfillmentMetricsResponse>> GetFulfillmentMetricsAsync(
        DateTime? startDate,
        DateTime? endDate)
    {
        if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
        {
            return Validation<FulfillmentMetricsResponse>(error ?? "Invalid date range.", null);
        }

        var currentData = await _analyticsRepository.GetFulfillmentOrdersAsync(start, end);
        var avgHoursToShip = CalculateAverageHours(currentData.Select(item => (item.OrderDate, item.ShippedDate)));
        var avgHoursToDeliver = CalculateAverageHours(currentData.Select(item => (item.OrderDate, item.DeliveredDate)));

        var periodDays = Math.Max(1, (end - start).TotalDays);
        var previousStart = start.AddDays(-periodDays);
        var previousEnd = start;

        var previousData = await _analyticsRepository.GetFulfillmentOrdersAsync(previousStart, previousEnd);
        var previousAvgShip = CalculateAverageHours(previousData.Select(item => (item.OrderDate, item.ShippedDate)));
        var trend = previousAvgShip <= 0 ? 0 : Math.Round(((avgHoursToShip - previousAvgShip) / previousAvgShip) * 100, 2);

        return ServiceResult<FulfillmentMetricsResponse>.Ok(new FulfillmentMetricsResponse
        {
            AvgHoursToShip = Math.Round((decimal)avgHoursToShip, 2),
            AvgHoursToDeliver = Math.Round((decimal)avgHoursToDeliver, 2),
            Trend = (decimal)trend
        });
    }

    public async Task<ServiceResult<CustomerBreakdownResponse>> GetCustomerBreakdownAsync(DateTime? startDate, DateTime? endDate)
    {
        if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
        {
            return Validation<CustomerBreakdownResponse>(error ?? "Invalid date range.", null);
        }

        var firstOrders = await _analyticsRepository.GetFirstOrdersByUserAsync();
        var ordersInRange = await _analyticsRepository.GetUserRevenueInRangeAsync(start, end);

        var firstOrderByUser = firstOrders
            .ToDictionary(item => item.UserId, item => item.FirstOrder, StringComparer.OrdinalIgnoreCase);

        var newCustomerIds = firstOrderByUser
            .Where(kvp => kvp.Value >= start && kvp.Value <= end)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var returningCustomerIds = ordersInRange
            .Select(o => o.UserId)
            .Where(userId => firstOrderByUser.TryGetValue(userId, out var firstOrderDate) && firstOrderDate < start)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newCustomerRevenue = ordersInRange
            .Where(o => newCustomerIds.Contains(o.UserId))
            .Sum(o => o.Total);

        var returningCustomerRevenue = ordersInRange
            .Where(o => returningCustomerIds.Contains(o.UserId))
            .Sum(o => o.Total);

        return ServiceResult<CustomerBreakdownResponse>.Ok(new CustomerBreakdownResponse
        {
            NewCustomers = newCustomerIds.Count,
            ReturningCustomers = returningCustomerIds.Count,
            NewCustomerRevenue = newCustomerRevenue,
            ReturningCustomerRevenue = returningCustomerRevenue
        });
    }

    public async Task<ServiceResult<IReadOnlyList<CustomerGrowthResponse>>> GetCustomerGrowthAsync(
        DateTime? startDate,
        DateTime? endDate,
        string granularity)
    {
        if (!TryNormalizeDateRange(startDate, endDate, out var start, out var end, out var error))
        {
            return Validation<IReadOnlyList<CustomerGrowthResponse>>(error ?? "Invalid date range.", null);
        }

        if (!TryNormalizeGranularity(granularity, out var normalizedGranularity))
        {
            return Validation<IReadOnlyList<CustomerGrowthResponse>>("Granularity must be day, week, or month.", null);
        }

        var baselineCount = await _userRepository.GetUserCountBeforeAsync(start);
        var registrations = await _userRepository.GetUserRegistrationsAsync(start, end);

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
        var result = new List<CustomerGrowthResponse>(grouped.Count);

        foreach (var entry in grouped)
        {
            runningTotal += entry.newRegistrations;
            result.Add(new CustomerGrowthResponse
            {
                Date = entry.date,
                NewRegistrations = entry.newRegistrations,
                CumulativeTotal = runningTotal
            });
        }

        return ServiceResult<IReadOnlyList<CustomerGrowthResponse>>.Ok(result);
    }

    public async Task<ServiceResult<IReadOnlyList<TopCustomerResponse>>> GetTopCustomersAsync(
        int limit,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (limit <= 0 || limit > 50)
        {
            return Validation<IReadOnlyList<TopCustomerResponse>>("Limit must be between 1 and 50.", null);
        }

        DateTime? start = null;
        DateTime? end = null;
        if (startDate != null || endDate != null)
        {
            if (!TryNormalizeDateRange(startDate, endDate, out var normalizedStart, out var normalizedEnd, out var error))
            {
                return Validation<IReadOnlyList<TopCustomerResponse>>(error ?? "Invalid date range.", null);
            }

            start = normalizedStart;
            end = normalizedEnd;
        }

        var topCustomers = await _analyticsRepository.GetTopCustomersAsync(limit, start, end);
        var userIds = topCustomers.Select(x => x.UserId).ToList();
        var users = await _userRepository.GetUsersByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

        var response = topCustomers.Select(customer =>
        {
            userLookup.TryGetValue(customer.UserId, out var user);
            var name = user == null
                ? null
                : string.Join(" ", new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

            return new TopCustomerResponse
            {
                UserId = customer.UserId,
                Email = user?.Email,
                Name = string.IsNullOrWhiteSpace(name) ? null : name,
                TotalSpent = customer.TotalSpent,
                OrderCount = customer.OrderCount,
                LastOrderDate = customer.LastOrderDate
            };
        }).ToList();

        return ServiceResult<IReadOnlyList<TopCustomerResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyList<WishlistedProductResponse>>> GetMostWishlistedAsync(int limit)
    {
        if (limit <= 0 || limit > 50)
        {
            return Validation<IReadOnlyList<WishlistedProductResponse>>("Limit must be between 1 and 50.", null);
        }

        var items = await _analyticsRepository.GetMostWishlistedAsync(limit);
        var response = items.Select(item => new WishlistedProductResponse
        {
            ProductId = item.ProductId,
            Name = item.Name,
            WishlistCount = item.WishlistCount
        }).ToList();

        return ServiceResult<IReadOnlyList<WishlistedProductResponse>>.Ok(response);
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

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);
}
