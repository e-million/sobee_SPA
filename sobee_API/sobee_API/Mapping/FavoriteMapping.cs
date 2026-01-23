using sobee_API.DTOs.Favorites;
using Sobee.Domain.Entities.Reviews;

namespace sobee_API.Mapping;

public static class FavoriteMapping
{
    public static FavoriteItemDto ToFavoriteItemDto(this Tfavorite favorite)
    {
        return new FavoriteItemDto
        {
            FavoriteId = favorite.IntFavoriteId,
            ProductId = favorite.IntProductId,
            Added = favorite.DtmDateAdded
        };
    }
}
