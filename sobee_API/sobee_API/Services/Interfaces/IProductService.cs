using sobee_API.Domain;
using sobee_API.DTOs.Cart;
using sobee_API.DTOs.Products;

namespace sobee_API.Services.Interfaces;

public interface IProductService
{
    Task<ServiceResult<ProductListResponseDto>> GetProductsAsync(
        string? query,
        string? category,
        int page,
        int pageSize,
        string? sort,
        bool isAdmin);
    Task<ServiceResult<ProductDetailDto>> GetProductAsync(int productId, bool isAdmin);
    Task<ServiceResult<ProductDetailDto>> CreateProductAsync(CreateProductRequest request);
    Task<ServiceResult<ProductDetailDto>> UpdateProductAsync(int productId, UpdateProductRequest request);
    Task<ServiceResult<MessageResponseDto>> DeleteProductAsync(int productId);
    Task<ServiceResult<ProductImageResponseDto>> AddProductImageAsync(int productId, AddProductImageRequest request);
    Task<ServiceResult<MessageResponseDto>> DeleteProductImageAsync(int productId, int imageId);
}
