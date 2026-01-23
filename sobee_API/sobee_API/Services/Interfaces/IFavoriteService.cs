using sobee_API.Domain;
using sobee_API.DTOs.Favorites;

namespace sobee_API.Services.Interfaces;

public interface IFavoriteService
{
    Task<ServiceResult<FavoriteListResponseDto>> GetFavoritesAsync(string? userId, int page, int pageSize);
    Task<ServiceResult<FavoriteAddResponseDto>> AddFavoriteAsync(string? userId, int productId);
    Task<ServiceResult<FavoriteRemoveResponseDto>> RemoveFavoriteAsync(string? userId, int productId);
}
