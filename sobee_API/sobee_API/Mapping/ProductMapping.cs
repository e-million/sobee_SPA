using System.Collections.Generic;
using System.Linq;
using sobee_API.DTOs.Products;
using Sobee.Domain.Entities.Products;

namespace sobee_API.Mapping;

public static class ProductMapping
{
    public static ProductListDto ToProductListDto(this Tproduct product, bool isAdmin)
    {
        return new ProductListDto
        {
            Id = product.IntProductId,
            Name = product.StrName,
            Description = product.strDescription,
            Price = product.DecPrice,
            Cost = isAdmin ? product.DecCost : null,
            InStock = product.IntStockAmount > 0,
            PrimaryImageUrl = product.GetPrimaryImageUrl(),
            StockAmount = isAdmin ? product.IntStockAmount : null,
            Category = product.IntDrinkCategory?.StrName,
            CategoryId = product.IntDrinkCategoryId,
            IsActive = isAdmin ? product.BlnIsActive : null
        };
    }

    public static ProductDetailDto ToProductDetailDto(this Tproduct product, bool isAdmin)
    {
        return new ProductDetailDto
        {
            Id = product.IntProductId,
            Name = product.StrName,
            Description = product.strDescription,
            Price = product.DecPrice,
            InStock = product.IntStockAmount > 0,
            StockAmount = isAdmin ? product.IntStockAmount : null,
            Category = product.IntDrinkCategory?.StrName,
            CategoryId = product.IntDrinkCategoryId,
            Cost = isAdmin ? product.DecCost : null,
            IsActive = isAdmin ? product.BlnIsActive : null,
            Images = (product.TproductImages ?? new List<TproductImage>())
                .OrderBy(i => i.IntProductImageId)
                .Select(ToProductImageDto)
                .ToList()
        };
    }

    public static ProductImageDto ToProductImageDto(this TproductImage image)
    {
        return new ProductImageDto
        {
            Id = image.IntProductImageId,
            Url = image.StrProductImageUrl
        };
    }

    public static ProductImageResponseDto ToProductImageResponseDto(this TproductImage image)
    {
        return new ProductImageResponseDto
        {
            Id = image.IntProductImageId,
            ProductId = image.IntProductId,
            Url = image.StrProductImageUrl
        };
    }

    public static string? GetPrimaryImageUrl(this Tproduct product)
    {
        if (product.TproductImages == null || product.TproductImages.Count == 0)
        {
            return null;
        }

        return product.TproductImages
            .OrderBy(i => i.IntProductImageId)
            .Select(i => i.StrProductImageUrl)
            .FirstOrDefault();
    }
}
