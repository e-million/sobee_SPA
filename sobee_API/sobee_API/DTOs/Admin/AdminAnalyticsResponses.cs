namespace sobee_API.DTOs.Admin
{
    public sealed class RevenueByPeriodResponse
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AvgOrderValue { get; set; }
    }

    public sealed class OrderStatusBreakdownResponse
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Paid { get; set; }
        public int Processing { get; set; }
        public int Shipped { get; set; }
        public int Delivered { get; set; }
        public int Cancelled { get; set; }
        public int Refunded { get; set; }
        public int Other { get; set; }
    }

    public sealed class RatingDistributionResponse
    {
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public RatingDistributionBreakdown Distribution { get; set; } = new();
    }

    public sealed class RatingDistributionBreakdown
    {
        public int OneStar { get; set; }
        public int TwoStar { get; set; }
        public int ThreeStar { get; set; }
        public int FourStar { get; set; }
        public int FiveStar { get; set; }
    }

    public sealed class RecentReviewResponse
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public bool HasReplies { get; set; }
    }

    public sealed class WorstProductResponse
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class CategoryPerformanceResponse
    {
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "Uncategorized";
        public int ProductCount { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class InventorySummaryResponse
    {
        public int TotalProducts { get; set; }
        public int InStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TotalStockValue { get; set; }
    }

    public sealed class FulfillmentMetricsResponse
    {
        public decimal AvgHoursToShip { get; set; }
        public decimal AvgHoursToDeliver { get; set; }
        public decimal Trend { get; set; }
    }

    public sealed class CustomerBreakdownResponse
    {
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public decimal NewCustomerRevenue { get; set; }
        public decimal ReturningCustomerRevenue { get; set; }
    }

    public sealed class CustomerGrowthResponse
    {
        public DateTime Date { get; set; }
        public int NewRegistrations { get; set; }
        public int CumulativeTotal { get; set; }
    }

    public sealed class TopCustomerResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public sealed class WishlistedProductResponse
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public int WishlistCount { get; set; }
    }
}
