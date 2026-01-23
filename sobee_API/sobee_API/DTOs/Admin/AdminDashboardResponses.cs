namespace sobee_API.DTOs.Admin
{
    public sealed class AdminSummaryResponse
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public sealed class OrdersPerDayResponse
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class LowStockProductResponse
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public int StockAmount { get; set; }
    }

    public sealed class TopProductResponse
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}
