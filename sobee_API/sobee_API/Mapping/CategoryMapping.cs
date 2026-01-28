using sobee_API.DTOs.Categories;
using Sobee.Domain.Entities.Products;

namespace sobee_API.Mapping;

public static class CategoryMapping
{
    public static CategoryResponseDto ToCategoryResponseDto(this TdrinkCategory category)
    {
        return new CategoryResponseDto
        {
            Id = category.IntDrinkCategoryId,
            Name = category.StrName,
            Description = string.IsNullOrWhiteSpace(category.StrDescription)
                ? null
                : category.StrDescription
        };
    }
}
