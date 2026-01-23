using sobee_API.Domain;
using sobee_API.DTOs.Favorites;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IProductRepository _productRepository;

    public FavoriteService(IFavoriteRepository favoriteRepository, IProductRepository productRepository)
    {
        _favoriteRepository = favoriteRepository;
        _productRepository = productRepository;
    }

    public async Task<ServiceResult<FavoriteListResponseDto>> GetFavoritesAsync(string? userId, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<FavoriteListResponseDto>("Missing NameIdentifier claim.", null);
        }

        if (page <= 0)
        {
            return Validation<FavoriteListResponseDto>("page must be >= 1", new { page });
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            return Validation<FavoriteListResponseDto>("pageSize must be between 1 and 100", new { pageSize });
        }

        var (items, totalCount) = await _favoriteRepository.GetByUserAsync(userId, page, pageSize);

        var response = new FavoriteListResponseDto
        {
            UserId = userId,
            Count = items.Count,
            Favorites = items.Select(MapFavorite).ToList(),
            TotalCount = totalCount
        };

        return ServiceResult<FavoriteListResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<FavoriteAddResponseDto>> AddFavoriteAsync(string? userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<FavoriteAddResponseDto>("Missing NameIdentifier claim.", null);
        }

        var productExists = await _productRepository.ExistsAsync(productId);
        if (!productExists)
        {
            return NotFound<FavoriteAddResponseDto>("Product not found.", new { productId });
        }

        var alreadyExists = await _favoriteRepository.ExistsAsync(userId, productId);
        if (alreadyExists)
        {
            return ServiceResult<FavoriteAddResponseDto>.Ok(new FavoriteAddResponseDto
            {
                Message = "Already favorited.",
                ProductId = productId
            });
        }

        var favorite = new Tfavorite
        {
            IntProductId = productId,
            DtmDateAdded = DateTime.UtcNow,
            UserId = userId
        };

        await _favoriteRepository.AddAsync(favorite);
        await _favoriteRepository.SaveChangesAsync();

        return ServiceResult<FavoriteAddResponseDto>.Ok(new FavoriteAddResponseDto
        {
            Message = "Favorited.",
            FavoriteId = favorite.IntFavoriteId,
            ProductId = favorite.IntProductId,
            Added = favorite.DtmDateAdded
        });
    }

    public async Task<ServiceResult<FavoriteRemoveResponseDto>> RemoveFavoriteAsync(string? userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<FavoriteRemoveResponseDto>("Missing NameIdentifier claim.", null);
        }

        var favorite = await _favoriteRepository.FindByUserAndProductAsync(userId, productId, track: true);
        if (favorite == null)
        {
            return NotFound<FavoriteRemoveResponseDto>("Favorite not found.", new { productId });
        }

        await _favoriteRepository.RemoveAsync(favorite);
        await _favoriteRepository.SaveChangesAsync();

        return ServiceResult<FavoriteRemoveResponseDto>.Ok(new FavoriteRemoveResponseDto
        {
            Message = "Unfavorited.",
            ProductId = productId
        });
    }

    private static FavoriteItemDto MapFavorite(Tfavorite favorite)
        => new()
        {
            FavoriteId = favorite.IntFavoriteId,
            ProductId = favorite.IntProductId,
            Added = favorite.DtmDateAdded
        };

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);

    private static ServiceResult<T> Unauthorized<T>(string message, object? data)
        => ServiceResult<T>.Fail("Unauthorized", message, data);
}
