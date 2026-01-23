using System.Collections.Generic;
using System.Linq;
using sobee_API.Domain;
using sobee_API.DTOs.Cart;
using sobee_API.DTOs.Products;
using sobee_API.Mapping;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ServiceResult<ProductListResponseDto>> GetProductsAsync(
        string? query,
        string? category,
        int page,
        int pageSize,
        string? sort,
        bool isAdmin)
    {
        if (page <= 0)
        {
            return Validation<ProductListResponseDto>("page must be >= 1", new { page });
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            return Validation<ProductListResponseDto>("pageSize must be between 1 and 100", new { pageSize });
        }

        var (items, totalCount) = await _productRepository.GetProductsAsync(
            query,
            category,
            page,
            pageSize,
            sort,
            track: false);

        var response = new ProductListResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.Select(p => p.ToProductListDto(isAdmin)).ToList()
        };

        return ServiceResult<ProductListResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<ProductDetailDto>> GetProductAsync(int productId, bool isAdmin)
    {
        var product = await _productRepository.FindByIdWithImagesAsync(productId, track: false);
        if (product == null)
        {
            return NotFound<ProductDetailDto>("Product not found.", null);
        }

        return ServiceResult<ProductDetailDto>.Ok(product.ToProductDetailDto(isAdmin));
    }

    public async Task<ServiceResult<ProductDetailDto>> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Tproduct
        {
            StrName = request.Name.Trim(),
            strDescription = request.Description?.Trim() ?? string.Empty,
            DecPrice = request.Price,
            DecCost = request.Cost,
            IntStockAmount = request.StockAmount,
            IntDrinkCategoryId = request.CategoryId
        };

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        return ServiceResult<ProductDetailDto>.Ok(product.ToProductDetailDto(isAdmin: true));
    }

    public async Task<ServiceResult<ProductDetailDto>> UpdateProductAsync(int productId, UpdateProductRequest request)
    {
        var product = await _productRepository.FindByIdAsync(productId);
        if (product == null)
        {
            return NotFound<ProductDetailDto>("Product not found.", null);
        }

        if (request.Name != null)
        {
            product.StrName = request.Name.Trim();
        }

        if (request.Description != null)
        {
            product.strDescription = request.Description.Trim();
        }

        if (request.Price.HasValue)
        {
            product.DecPrice = request.Price.Value;
        }

        if (request.Cost.HasValue)
        {
            product.DecCost = request.Cost.Value;
        }

        if (request.StockAmount.HasValue)
        {
            product.IntStockAmount = request.StockAmount.Value;
        }

        if (request.CategoryId.HasValue)
        {
            product.IntDrinkCategoryId = request.CategoryId.Value;
        }

        await _productRepository.SaveChangesAsync();

        return ServiceResult<ProductDetailDto>.Ok(product.ToProductDetailDto(isAdmin: true));
    }

    public async Task<ServiceResult<MessageResponseDto>> DeleteProductAsync(int productId)
    {
        var product = await _productRepository.FindByIdWithImagesAsync(productId, track: true);
        if (product == null)
        {
            return NotFound<MessageResponseDto>("Product not found.", null);
        }

        if (product.TproductImages != null && product.TproductImages.Count > 0)
        {
            foreach (var image in product.TproductImages.ToList())
            {
                await _productRepository.RemoveImageAsync(image);
            }
        }

        await _productRepository.RemoveAsync(product);
        await _productRepository.SaveChangesAsync();

        return ServiceResult<MessageResponseDto>.Ok(new MessageResponseDto { Message = "Product deleted." });
    }

    public async Task<ServiceResult<ProductImageResponseDto>> AddProductImageAsync(int productId, AddProductImageRequest request)
    {
        var exists = await _productRepository.ExistsAsync(productId);
        if (!exists)
        {
            return NotFound<ProductImageResponseDto>("Product not found.", null);
        }

        var image = new TproductImage
        {
            IntProductId = productId,
            StrProductImageUrl = request.Url.Trim()
        };

        await _productRepository.AddImageAsync(image);
        await _productRepository.SaveChangesAsync();

        return ServiceResult<ProductImageResponseDto>.Ok(image.ToProductImageResponseDto());
    }

    public async Task<ServiceResult<MessageResponseDto>> DeleteProductImageAsync(int productId, int imageId)
    {
        var image = await _productRepository.FindImageAsync(productId, imageId);
        if (image == null)
        {
            return NotFound<MessageResponseDto>("Image not found for that product.", null);
        }

        await _productRepository.RemoveImageAsync(image);
        await _productRepository.SaveChangesAsync();

        return ServiceResult<MessageResponseDto>.Ok(new MessageResponseDto { Message = "Image deleted." });
    }

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);
}
