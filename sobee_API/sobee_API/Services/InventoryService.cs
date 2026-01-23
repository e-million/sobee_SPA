using System.Linq;
using sobee_API.Domain;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IProductRepository _productRepository;

    public InventoryService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ServiceResult<bool>> ValidateAndDecrementAsync(IReadOnlyList<InventoryRequestItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return ServiceResult<bool>.Ok(true);
        }

        var grouped = items
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(i => i.Quantity) })
            .ToList();

        var products = await _productRepository.GetByIdsAsync(grouped.Select(g => g.ProductId), track: true);
        var productMap = products.ToDictionary(p => p.IntProductId);

        foreach (var entry in grouped)
        {
            if (!productMap.TryGetValue(entry.ProductId, out var product))
            {
                return ServiceResult<bool>.Fail(
                    "ValidationError",
                    "Cart contains an item with missing product reference.",
                    new { productId = entry.ProductId });
            }

            var stockCheck = StockValidator.Validate(product.IntStockAmount, entry.Quantity);
            if (!stockCheck.IsValid)
            {
                return ServiceResult<bool>.Fail(
                    "InsufficientStock",
                    "Insufficient stock.",
                    new
                    {
                        productId = product.IntProductId,
                        productName = product.StrName,
                        requested = entry.Quantity,
                        availableStock = product.IntStockAmount
                    });
            }
        }

        foreach (var entry in grouped)
        {
            var product = productMap[entry.ProductId];
            product.IntStockAmount -= entry.Quantity;
        }

        return ServiceResult<bool>.Ok(true);
    }
}
