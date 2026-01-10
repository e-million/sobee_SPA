namespace sobee_API.DTOs
{
    public class ProductListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }

        // Stock is now int (matches Tproduct.IntStockAmount)
        public int Stock { get; set; }
    }

}
